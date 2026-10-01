using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Corpo de primeira pessoa, sem colisores, com pernas articuladas e passada proporcional ao deslocamento.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(250)]
    public sealed class PlayerBodyVisual : MonoBehaviour
    {
        private const string BodyName = "CorpoDoVisitante";
        private const float ReferenceHeight = 1.75f;
        private const float HipHeight = .91f;
        private const float ThighLength = .425f;
        private const float ShinLength = .415f;

        private sealed class Leg
        {
            public float side;
            public Transform thigh;
            public Transform knee;
            public Transform shin;
            public Transform shoe;
        }

        private CharacterController controller;
        private Transform eye;
        private Transform body;
        private Leg leftLeg;
        private Leg rightLeg;
        private sealed class Arm
        {
            public float side;
            public Transform upper, lower, hand;
        }
        private Arm leftArm, rightArm;
        private Transform torso;
        private Mesh roundedMesh;
        private Mesh torsoMesh;
        private Material trousersMaterial;
        private Material shirtMaterial;
        private Material shoesMaterial;
        private Material skinMaterial;
        private Vector3 previousPosition;
        private Vector3 travelDirection = Vector3.forward;
        private float gaitPhase;
        private float gaitWeight;
        private const float CycleDistance = 1.1f;
        public event System.Action<float, Vector3, float> FootPlanted;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            var playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera != null) eye = playerCamera.transform;

            // Retire a cápsula antiga caso um prefab ainda a contenha.
            var previousBody = transform.Find(BodyName);
            if (previousBody != null)
            {
                previousBody.gameObject.SetActive(false);
                DestroyOwned(previousBody.gameObject);
            }

            roundedMesh = CreateRoundedMesh();
            trousersMaterial = CreateMaterial("Visitante_Calca", new Color(.24f, .29f, .34f), .12f);
            shirtMaterial = CreateMaterial("Visitante_Camisa", new Color(.16f, .28f, .27f), .16f);
            shoesMaterial = CreateMaterial("Visitante_Sapatos", new Color(.055f, .065f, .07f), .28f);
            skinMaterial = CreateMaterial("Visitante_Pele", new Color(.48f, .30f, .21f), .18f);

            body = new GameObject(BodyName).transform;
            body.SetParent(transform, false);
            body.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            UpdateBodyOrigin();

            CreatePart("Quadril", body, new Vector3(0, .93f, -.025f), new Vector3(.37f, .30f, .29f), trousersMaterial);
            leftLeg = CreateLeg("Esquerda", -1f);
            rightLeg = CreateLeg("Direita", 1f);

            // Camisa contínua da cintura aos ombros, conectada ao quadril.
            torsoMesh = CreateTorsoMesh();
            torso = CreatePart("Tronco", body, Vector3.zero, Vector3.one, shirtMaterial);
            torso.GetComponent<MeshFilter>().sharedMesh = torsoMesh;
            // Só a cabeça fica invisível para a câmera situada nos olhos.
            CreatePart("Cabeca", body, new Vector3(0, 1.60f, 0), new Vector3(.20f, .25f, .21f), skinMaterial, true);
            leftArm = CreateArm(-1f);
            rightArm = CreateArm(1f);
            PoseUpperBody();
            PoseLeg(leftLeg, 0f);
            PoseLeg(rightLeg, Mathf.PI);
            previousPosition = transform.position;
        }

        private void OnEnable()
        {
            if (body != null) body.gameObject.SetActive(true);
            previousPosition = transform.position;
            gaitWeight = 0f;
        }

        private void OnDisable()
        {
            if (body != null) body.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (body == null) return;
            float deltaTime = Time.deltaTime;
            var displacement = transform.position - previousPosition;
            previousPosition = transform.position;
            bool teleported = displacement.sqrMagnitude > .75f * .75f;
            displacement.y = 0f;

            if (eye != null)
            {
                var heading = Vector3.ProjectOnPlane(eye.forward, Vector3.up);
                // Olhar quase vertical não fornece um rumo estável; mantenha o último.
                if (heading.sqrMagnitude > .04f)
                    body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(heading, Vector3.up),
                        1f - Mathf.Exp(-10f * deltaTime));
            }

            if (teleported || deltaTime <= 0f)
            {
                gaitWeight = 0f;
                gaitPhase = 0f;
            }
            else
            {
                float distance = displacement.magnitude;
                float speed = distance / deltaTime;
                bool grounded = controller == null || (controller.enabled && controller.isGrounded);
                if (!grounded) { distance = 0f; speed = 0f; }
                float targetWeight = Mathf.Clamp01(speed / .25f);
                gaitWeight = Mathf.MoveTowards(gaitWeight, targetWeight, deltaTime * 6f);
                if (distance > .0001f)
                {
                    travelDirection = body.InverseTransformDirection(displacement / distance);
                    travelDirection.y = 0f;
                    travelDirection.Normalize();
                    float nextPhase = gaitPhase + distance * (Mathf.PI * 2f / CycleDistance);
                    if (gaitPhase < Mathf.PI && nextPhase >= Mathf.PI)
                        FootPlanted?.Invoke(1f, rightLeg.shoe.position, speed);
                    if (nextPhase >= Mathf.PI * 2f)
                        FootPlanted?.Invoke(-1f, leftLeg.shoe.position, speed);
                    gaitPhase = Mathf.Repeat(nextPhase, Mathf.PI * 2f);
                }
            }

            UpdateBodyOrigin();
            PoseLeg(leftLeg, gaitPhase);
            PoseLeg(rightLeg, gaitPhase + Mathf.PI);
            PoseUpperBody();
        }

        private void UpdateBodyOrigin()
        {
            // O pivô do Player nem sempre fica na sola: use a base real do controller.
            float floor = controller != null ? controller.center.y - controller.height * .5f : 0f;
            float scale = controller != null ? controller.height / ReferenceHeight : 1f;
            // A cabeça fica levemente à frente do quadril. Com ambos no mesmo
            // eixo, as coxas eram vistas como duas tampas que escondiam os pés.
            body.localPosition = new Vector3(0, floor, 0) +
                transform.InverseTransformVector(-body.forward * .18f);
            body.localScale = Vector3.one * scale;
        }

        private Leg CreateLeg(string suffix, float side)
        {
            return new Leg
            {
                side = side,
                thigh = CreatePart("Coxa" + suffix, body, Vector3.zero, Vector3.one, trousersMaterial),
                knee = CreatePart("Joelho" + suffix, body, Vector3.zero, new Vector3(.14f, .145f, .15f), trousersMaterial),
                shin = CreatePart("Canela" + suffix, body, Vector3.zero, Vector3.one, trousersMaterial),
                shoe = CreatePart("Sapato" + suffix, body, Vector3.zero, new Vector3(.13f, .11f, .29f), shoesMaterial)
            };
        }

        private void PoseLeg(Leg leg, float phase)
        {
            var hip = new Vector3(leg.side * .105f, HipHeight - .035f * gaitWeight, -.025f);
            var ankle = new Vector3(leg.side * .105f, .095f, 0f);
            float cycle = Mathf.Repeat(phase / (Mathf.PI * 2f), 1f);
            bool stance = cycle < .5f;
            float progress = stance ? cycle * 2f : (cycle - .5f) * 2f;
            // O apoio recua linearmente com o deslocamento; o balanço suaviza a troca.
            float forward = stance ? Mathf.Lerp(1f,-1f,progress) : Mathf.Lerp(-1f,1f,Mathf.SmoothStep(0f,1f,progress));
            ankle += travelDirection * (forward * CycleDistance * .25f * gaitWeight);
            if (!stance) ankle.y += Mathf.Sin(progress * Mathf.PI) * .095f * gaitWeight;

            var axis = ankle - hip;
            float distance = Mathf.Clamp(axis.magnitude, .01f, ThighLength + ShinLength - .001f);
            var direction = axis.normalized;
            ankle = hip + direction * distance;
            // IK de dois ossos: o joelho flexiona à frente e a sola nunca entra no piso.
            float along = (ThighLength * ThighLength - ShinLength * ShinLength + distance * distance) / (2f * distance);
            float bend = Mathf.Sqrt(Mathf.Max(0f, ThighLength * ThighLength - along * along));
            var kneeForward = Vector3.ProjectOnPlane(Vector3.forward, direction).normalized;
            var knee = hip + direction * along + kneeForward * bend;
            PoseSegment(leg.thigh, hip, knee, .175f, .19f);
            PoseSegment(leg.shin, knee, ankle, .125f, .14f);
            leg.knee.localPosition = knee;
            leg.shoe.localPosition = ankle + new Vector3(0f, -.04f, .07f);
            float pitch = stance ? Mathf.Lerp(-9f,0f,Mathf.Clamp01(progress*5f)) + 16f*Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.75f,1f,progress))
                : Mathf.Lerp(16f,-9f,Mathf.SmoothStep(0f,1f,progress));
            pitch *= gaitWeight * travelDirection.z;
            leg.shoe.localRotation = Quaternion.Euler(pitch,0f,0f);
            float radians=pitch*Mathf.Deg2Rad;
            float soleClearance=.055f*Mathf.Abs(Mathf.Cos(radians))+.145f*Mathf.Abs(Mathf.Sin(radians));
            var shoePosition=leg.shoe.localPosition;
            shoePosition.y=Mathf.Max(shoePosition.y,soleClearance);
            leg.shoe.localPosition=shoePosition;
        }

        private static void PoseSegment(Transform segment, Vector3 start, Vector3 end, float width, float depth)
        {
            var delta = end - start;
            segment.localPosition = (start + end) * .5f;
            segment.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
            // As extremidades arredondadas se sobrepõem ligeiramente às articulações.
            segment.localScale = new Vector3(width, delta.magnitude + .08f, depth);
        }

        private Arm CreateArm(float side)
        {
            string suffix = side < 0f ? "Esquerdo" : "Direito";
            return new Arm
            {
                side = side,
                upper = CreatePart("Braco" + suffix, body, Vector3.zero, Vector3.one, shirtMaterial),
                lower = CreatePart("Antebraco" + suffix, body, Vector3.zero, Vector3.one, skinMaterial),
                hand = CreatePart("Mao" + suffix, body, Vector3.zero, new Vector3(.085f,.14f,.065f), skinMaterial)
            };
        }

        private void PoseUpperBody()
        {
            // Mesma fase e peso das pernas: só anima com deslocamento real e
            // desacelera junto delas. Nenhum balanço é aplicado à câmera VR.
            float swing = Mathf.Cos(gaitPhase) * gaitWeight;
            var pivot = new Vector3(0, 1f, -.025f);
            var rotation = Quaternion.Euler(1.5f * gaitWeight, -3f * swing, 1f * swing);
            var bob = Vector3.up * (Mathf.Cos(gaitPhase * 2f) * .008f * gaitWeight);
            torso.localRotation = rotation;
            torso.localPosition = pivot - rotation * pivot + bob;
            PoseArm(leftArm, swing, rotation, pivot, bob);
            PoseArm(rightArm, -swing, rotation, pivot, bob);
        }

        private static void PoseArm(Arm arm, float swing, Quaternion torsoRotation, Vector3 pivot, Vector3 bob)
        {
            Vector3 FollowTorso(Vector3 p) => pivot + torsoRotation * (p - pivot) + bob;
            var shoulder = new Vector3(arm.side * .23f, 1.40f, -.03f);
            var upperRotation = Quaternion.Euler(swing * 18f, 0, 0);
            var elbow = shoulder + upperRotation * new Vector3(arm.side * .055f, -.28f, .03f);
            var hand = elbow + Quaternion.Euler(swing * 22f, 0, 0) * new Vector3(-arm.side * .015f, -.21f, .055f);
            PoseSegment(arm.upper, FollowTorso(shoulder), FollowTorso(elbow), .12f, .12f);
            PoseSegment(arm.lower, FollowTorso(elbow), FollowTorso(hand), .095f, .095f);
            arm.hand.localPosition = FollowTorso(hand - Vector3.up * .025f);
            arm.hand.localRotation = torsoRotation * Quaternion.Euler(swing * 22f, 0, 0);
        }

        private static Mesh CreateTorsoMesh()
        {
            const int sides = 20;
            // y, meia largura, meia profundidade. A cintura sobrepõe a bacia.
            var rings = new[] { new Vector3(.99f,.17f,.125f), new Vector3(1.07f,.18f,.135f),
                new Vector3(1.27f,.205f,.14f), new Vector3(1.41f,.215f,.13f),
                new Vector3(1.48f,.15f,.09f), new Vector3(1.50f,.075f,.07f) };
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            foreach (var ring in rings)
                for (int s=0;s<=sides;s++)
                {
                    float angle=s*Mathf.PI*2/sides;
                    vertices.Add(new Vector3(Mathf.Cos(angle)*ring.y,ring.x,Mathf.Sin(angle)*ring.z-.035f));
                }
            for(int r=0;r<rings.Length-1;r++) for(int s=0;s<sides;s++)
            {
                int a=r*(sides+1)+s,b=a+sides+1;
                triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            for(int s=1;s<sides-1;s++)
            {
                triangles.AddRange(new[]{0,s,s+1});
                int top=(rings.Length-1)*(sides+1);
                triangles.AddRange(new[]{top,top+s+1,top+s});
            }
            var mesh=new Mesh { name="Visitante_CamisaContinua" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private Transform CreatePart(string partName, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool shadowOnly = false)
        {
            var part = new GameObject(partName);
            part.layer = gameObject.layer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = roundedMesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadowOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return part.transform;
        }

        private static Material CreateMaterial(string materialName, Color color, float smoothness)
        {
            var material = new Material(Shader.Find("Standard")) { name = materialName, color = color };
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_GlossyReflections", 0f);
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            return material;
        }

        private static Mesh CreateRoundedMesh()
        {
            // Uma malha compartilhada de 192 triângulos: contorno redondo sem custo de rig/skinning.
            const int sides = 12;
            const int rings = 8;
            var vertices = new Vector3[(sides + 1) * (rings + 1)];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[sides * rings * 6];
            int index = 0;
            for (int ring = 0; ring <= rings; ring++)
            {
                float latitude = Mathf.PI * ring / rings;
                for (int side = 0; side <= sides; side++)
                {
                    float longitude = 2f * Mathf.PI * side / sides;
                    var normal = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude),
                        Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                    int vertex = ring * (sides + 1) + side;
                    vertices[vertex] = normal * .5f;
                    normals[vertex] = normal;
                    if (ring == rings || side == sides) continue;
                    triangles[index++] = vertex;
                    triangles[index++] = vertex + 1;
                    triangles[index++] = vertex + sides + 1;
                    triangles[index++] = vertex + 1;
                    triangles[index++] = vertex + sides + 2;
                    triangles[index++] = vertex + sides + 1;
                }
            }
            var mesh = new Mesh { name = "Visitante_FormaArredondada", vertices = vertices, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (body != null) DestroyOwned(body.gameObject);
            DestroyOwned(roundedMesh);
            DestroyOwned(torsoMesh);
            DestroyOwned(trousersMaterial);
            DestroyOwned(shirtMaterial);
            DestroyOwned(shoesMaterial);
            DestroyOwned(skinMaterial);
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
