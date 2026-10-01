using UnityEngine;

namespace MuseumModerna
{
    /// <summary>
    /// Controla o movimento do player baseado em "olhar para baixo".
    /// Quando GyroscopeController.IsLookingDown == true, o player caminha
    /// para frente no plano XZ (sem subir/descer).
    ///
    /// Como usar:
    ///   1. Adicione este script ao GameObject raiz do Player.
    ///   2. O Player deve ter um CharacterController anexado.
    ///   3. Arraste o GyroscopeController para o campo gyroController no Inspector.
    /// </summary>
    [DefaultExecutionOrder(50)] // depois do GyroscopeController (0): usa a rotação deste frame
    public class HeadGazeMovement : MonoBehaviour
    {
        // ─── Configurações no Inspector ───────────────────────────────────────

        [Header("Movimento")]
        [Tooltip("Velocidade de caminhada em m/s quando IsLookingDown = true")]
        [SerializeField] private float walkSpeed = 1.25f;

        [Tooltip("Fator de desaceleração ao parar de olhar para baixo (maior = para mais rápido)")]
        [SerializeField] private float deceleration = 14f;

        [Tooltip("Fator de aceleração ao começar a olhar para baixo (maior = acelera mais rápido)")]
        [SerializeField] private float acceleration = 14f;

        [Header("Gravidade")]
        [Tooltip("Força da gravidade aplicada ao CharacterController")]
        [SerializeField] private float gravity = -9.81f;

        [Header("Referências")]
        [Tooltip("Referência ao GyroscopeController da câmera do player")]
        [SerializeField] private GyroscopeController gyroController;

        [Tooltip("Transform da câmera — usado para obter a direção de olhar horizontal")]
        [SerializeField] private Transform cameraTransform;

        [Header("Proteção de Obras")]
        [Tooltip("Distância mínima até uma obra quando o visitante caminha na direção dela.")]
        [SerializeField] private float exhibitStopDistance = 0.72f;

        [Tooltip("Raio usado para antecipar a colisão com quadros, esculturas e vitrines.")]
        [SerializeField] private float exhibitStopRadius = 0.28f;

        // ─── Variáveis Privadas ────────────────────────────────────────────────

        // Referência ao CharacterController deste objeto
        private CharacterController _characterController;

        // Velocidade atual no plano XZ (com inércia)
        private float _currentSpeed = 0f;

        // Velocidade vertical para gravidade
        private float _verticalVelocity = 0f;

        // Flag para permitir/bloquear movimento (ex: durante cutscenes)
        private bool _movementEnabled = true;
        private bool _applicationPaused;
        private bool _applicationUnfocused;
        private bool _menuWasOpen;

        // Última direção horizontal válida — usada quando o olhar está quase a pino
        private Vector3 _lastFlatDir = Vector3.forward;
        private readonly RaycastHit[] _exhibitHits = new RaycastHit[8];

        // ─── Ciclo de Vida Unity ──────────────────────────────────────────────

        private void Start()
        {
            _lastFlatDir = FlattenOrKeep(transform.forward, transform.forward);
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // minMoveDistance > 0 faz o CharacterController DESCARTAR deslocamentos pequenos —
            // justamente os dos primeiros frames de aceleração e os de 30fps no celular.
            // Cria o CharacterController automaticamente se não existir na cena
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
                _characterController.height = 1.8f;
                _characterController.radius = 0.3f;
                _characterController.center = new Vector3(0f, 0.9f, 0f);
                _characterController.slopeLimit = 45f;
                _characterController.stepOffset = 0.3f;
                Debug.Log("[MuseumModerna] CharacterController adicionado automaticamente ao Player.");
            }
            _characterController.minMoveDistance = 0f;
            _characterController.skinWidth = Mathf.Clamp(_characterController.radius * .1f, .01f, .04f);
            _characterController.stepOffset = Mathf.Min(_characterController.stepOffset, .2f);

            // Se a câmera não foi definida, tenta encontrar a câmera principal
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
                else
                {
                    Debug.LogError("[MuseumModerna] HeadGazeMovement: nenhum cameraTransform definido!");
                }
            }

            // Tenta encontrar GyroscopeController automaticamente se não definido
            if (gyroController == null)
            {
                gyroController = GetComponentInChildren<GyroscopeController>();
                if (gyroController == null)
                {
                    Debug.LogError("[MuseumModerna] HeadGazeMovement: GyroscopeController não encontrado!");
                }
            }
        }

        private void Update()
        {
            if (_characterController == null || !_characterController.enabled ||
                !_characterController.gameObject.activeInHierarchy || _applicationPaused || _applicationUnfocused)
            {
                ResetMovement();
                return;
            }
            // Atualiza movimento horizontal somente se habilitado
            if (_movementEnabled)
            {
                UpdateHorizontalMovement();
            }
            else
            {
                // Menus e pausas interrompem a caminhada imediatamente.
                DecelerateToStop();
            }

            // Um único Move preserva o contato com o chão. Bloquear o olhar, abrir
            // um menu ou chegar perto de uma obra nunca pode suspender a gravidade.
            ApplyGravity();
            Vector3 direction = cameraTransform != null ? GetFlatLookDirection() : _lastFlatDir;
            Vector3 displacement = direction * (_currentSpeed * Time.deltaTime);
            displacement.y = _verticalVelocity * Time.deltaTime;
            CollisionFlags collision = _characterController.Move(displacement);
            if ((collision & CollisionFlags.Below) != 0 && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            if ((collision & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
        }

        // ─── Lógica de Movimento ──────────────────────────────────────────────

        /// <summary>
        /// Atualiza a velocidade e posição horizontal baseada no estado do giroscópio.
        /// </summary>
        private void UpdateHorizontalMovement()
        {
            if (gyroController == null || cameraTransform == null)
            {
                _currentSpeed = 0f;
                return;
            }

            var vr = MobileVrMode.Instance;
            bool inVr = vr != null && vr.IsActive;
            bool gazeBlocked = inVr && vr.WorldGuide != null &&
                (vr.WorldGuide.ConsumesGaze || vr.WorldGuide.MenuOpen);
            bool menuOpen = inVr && vr.WorldGuide != null && vr.WorldGuide.MenuOpen;
            if (menuOpen != _menuWasOpen)
            {
                _menuWasOpen = menuOpen;
                ResetMovement();
            }
            // Ler uma ficha suspende o avanço, mas não exige rearmar o gesto
            // quando o olhar sai dela. Menus e pausas continuam exigindo olhar neutro.
            if (gazeBlocked) { _currentSpeed = 0f; return; }
            bool isWalking = gyroController.IsLookingDown;
            float speed = inVr ? Mathf.Min(walkSpeed, 1.1f) : walkSpeed;

            if (isWalking)
            {
                // Acelera suavemente até walkSpeed
                _currentSpeed = Damp(_currentSpeed, speed, acceleration);
            }
            else
            {
                // No visor, levantar a cabeça deve parar a câmera sem continuar deslizando.
                _currentSpeed = inVr ? 0f : Damp(_currentSpeed, 0f, deceleration);

                // Evita vibração em velocidades muito baixas (threshold)
                if (_currentSpeed < 0.01f) _currentSpeed = 0f;
            }

            // Direção de movimento no plano XZ (nunca sobe/desce)
            Vector3 lookDir = GetFlatLookDirection();

            // A colisão física ainda resolve paredes e pedestais; esta barreira
            // semitransparente só interrompe a caminhada ao chegar perto de uma
            // obra na direção do olhar, sem prender o visitante ao se afastar.
            if (_currentSpeed > 0f && IsApproachingExhibit(lookDir))
            {
                // O obstáculo bloqueia somente o avanço. Desarmar o gesto aqui
                // prendia o visitante mesmo depois de virar para um caminho livre.
                _currentSpeed = 0f;
                return;
            }

        }

        /// <summary>
        /// Direção horizontal para onde o player caminha.
        ///
        /// Só projetar cameraTransform.forward no plano XZ não basta: para andar o usuário
        /// baixa muito a cabeça, e perto de 90° a projeção tende a zero — Normalize() devolve
        /// Vector3.zero (o player trava) e, logo antes disso, qualquer tremida do celular
        /// gira a direção inteira. Mantemos o último rumo válido perto da vertical: usar
        /// transform.up ali fazia o roll do celular virar a caminhada lateralmente.
        /// </summary>
        private Vector3 GetFlatLookDirection()
        {
            Vector3 flat = cameraTransform.forward;
            flat.y = 0f;

            // sqrMagnitude < 0.03 ≈ olhando mais de 80° para baixo ou para cima
            if (flat.sqrMagnitude < 0.03f)
            {
                return _lastFlatDir;
            }

            _lastFlatDir = FlattenOrKeep(flat, _lastFlatDir);
            return _lastFlatDir;
        }

        /// <summary>Normaliza no plano XZ; se o vetor for degenerado, mantém o anterior.</summary>
        private static Vector3 FlattenOrKeep(Vector3 candidate, Vector3 fallback)
        {
            candidate.y = 0f;
            if (candidate.sqrMagnitude > 1e-4f) return candidate.normalized;

            fallback.y = 0f;
            return fallback.sqrMagnitude > 1e-4f ? fallback.normalized : Vector3.forward;
        }

        private bool IsApproachingExhibit(Vector3 direction)
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            int count = Physics.SphereCastNonAlloc(origin, exhibitStopRadius, direction, _exhibitHits,
                exhibitStopDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (_exhibitHits[i].collider != null &&
                    _exhibitHits[i].collider.GetComponentInParent<PaintingExhibit>() != null)
                    return true;
            return false;
        }

        /// <summary>
        /// Interpolação exponencial independente do framerate.
        /// Mathf.Lerp(a, b, k * deltaTime) acelera/freia em ritmos diferentes a 30 e a 60fps —
        /// e o celular oscila entre os dois.
        /// </summary>
        private static float Damp(float current, float target, float sharpness)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }

        /// <summary>
        /// Desacelera o player até parar (quando movimento está desabilitado).
        /// </summary>
        private void DecelerateToStop()
        {
            _currentSpeed = 0f;
        }

        /// <summary>
        /// A altura vem dos colisores, inclusive ao descer de um degrau ou após um teleporte.
        /// </summary>
        private void ApplyGravity()
        {
            if (_characterController != null && _characterController.isGrounded)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity + gravity * Time.deltaTime, -30f);
            }
        }

        // ─── API Pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Habilita ou desabilita o movimento do player.
        /// Use para travar durante cutscenes, menus, ou ao visualizar um quadro.
        /// </summary>
        /// <param name="enabled">True para permitir movimento, false para bloquear.</param>
        public void SetMovementEnabled(bool enabled)
        {
            if (_movementEnabled != enabled || !enabled) ResetMovement();
            _movementEnabled = enabled;

            if (!enabled)
            {
                Debug.Log("[MuseumModerna] Movimento do player desabilitado.");
            }
        }

        /// <summary>Limpa inércia e gesto ao entrar/sair de VR, recentrar ou retomar a visita.</summary>
        public void ResetMovement()
        {
            _currentSpeed = _verticalVelocity = 0f;
            gyroController?.ResetWalkingGesture();
            // Uma câmera vertical não fornece rumo confiável; mantenha o último rumo nesses casos.
            if (cameraTransform != null && Mathf.Abs(cameraTransform.forward.y) < 0.98f)
                _lastFlatDir = FlattenOrKeep(cameraTransform.forward, _lastFlatDir);
        }

        private void OnApplicationPause(bool paused)
        {
            _applicationPaused = paused;
            ResetMovement();
        }

        private void OnApplicationFocus(bool focused)
        {
            _applicationUnfocused = !focused;
            ResetMovement();
        }

        private void OnDisable() => ResetMovement();

        /// <summary>
        /// Limpa a inércia após um teleporte. Nome mantido para cenas e chamadas existentes;
        /// a posição vertical agora é resolvida pela gravidade e pelo chão.
        /// </summary>
        public void SyncLockedY()
        {
            ResetMovement();
        }

        /// <summary>
        /// Retorna verdadeiro se o player está ativamente se movendo.
        /// </summary>
        public bool IsMoving => _currentSpeed > 0.05f;

        /// <summary>
        /// Retorna a velocidade atual do player em m/s.
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        // ─── Debug Visual ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (cameraTransform == null) return;

            // Mostra a direção de movimento projetada no plano XZ
            Vector3 flatForward = cameraTransform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, flatForward * 2f);
        }
#endif
    }
}
