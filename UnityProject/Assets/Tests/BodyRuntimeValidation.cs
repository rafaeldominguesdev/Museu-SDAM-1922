#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Verifica a escala real, a passada, a silhueta e o descarte do corpo na cena salva.</summary>
    public sealed class BodyRuntimeValidation : MonoBehaviour
    {
        private int checks;

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 8; frame++) yield return null;
            var routine = Tests();
            while (true)
            {
                object value;
                try { if (!routine.MoveNext()) break; value = routine.Current; }
                catch (Exception exception) { Debug.LogException(exception); UnityEditor.EditorApplication.Exit(1); yield break; }
                yield return value;
            }
            Debug.Log($"[BodyValidation] PASS: {checks} verificações de escala, pernas, sombra e recursos.");
            UnityEditor.EditorApplication.Exit(0);
        }

        private IEnumerator Tests()
        {
            var player = FindAnyObjectByType<PlayerController>();
            Require(player != null, "Visitante existe na cena");
            var controller = player.GetComponent<CharacterController>();
            var visual = player.GetComponent<PlayerBodyVisual>();
            var eye = player.GetComponentInChildren<Camera>();
            Require(visual != null && controller != null && eye != null, "Corpo, cápsula e câmera disponíveis");
            Physics.SyncTransforms();
            float feetY = controller.bounds.min.y;
            Require(Mathf.Abs(feetY) < .07f && eye.transform.position.y - feetY > 1.5f &&
                eye.transform.position.y - feetY < 1.8f, "Pés apoiados e câmera entre 1,5 e 1,8 m do piso");
            Debug.Log($"[BodyValidation] Medidas: pés={feetY:F3} m; olhos={eye.transform.position.y:F3} m; cápsula={controller.bounds.size.y:F3} m.");

            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled = false;
            foreach (var gaze in FindObjectsByType<GazeDwellInteraction>(FindObjectsSortMode.None)) gaze.enabled = false;
            foreach (var cycle in FindObjectsByType<MuseumDayNightCycle>(FindObjectsSortMode.None)) cycle.enabled = false;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            player.enabled = false;
            Time.captureDeltaTime = 1f / 60f;
            var body = player.transform.Find("CorpoDoVisitante");
            Require(body != null && body.GetComponentsInChildren<Collider>(true).Length == 0 &&
                body.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "Visual não adiciona colisores, bloqueios do olhar nem corpos físicos ao visitante");
            var renderers = body.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Require(bounds.size.y > 1.6f && bounds.size.y < 1.85f && bounds.size.x < .8f,
                "Silhueta tem altura e largura humanas");
            var leftShoe = body.Find("SapatoEsquerda");
            var rightShoe = body.Find("SapatoDireita");
            Require(leftShoe != null && rightShoe != null &&
                leftShoe.GetComponent<Renderer>().bounds.max.x < rightShoe.GetComponent<Renderer>().bounds.min.x,
                "Duas pernas terminam em pés separados, sem cápsula central visível");
            Require(Mathf.Abs(leftShoe.GetComponent<Renderer>().bounds.min.y - feetY) < .025f &&
                Mathf.Abs(rightShoe.GetComponent<Renderer>().bounds.min.y - feetY) < .025f,
                "Solas coincidem com a base real do CharacterController");
            Require(body.Find("Tronco").GetComponent<Renderer>().shadowCastingMode == ShadowCastingMode.On &&
                body.Find("Quadril").GetComponent<Renderer>().bounds.Intersects(body.Find("Tronco").GetComponent<Renderer>().bounds) &&
                body.Find("Cabeca").GetComponent<Renderer>().shadowCastingMode == ShadowCastingMode.ShadowsOnly,
                "Tronco visível conectado à bacia; cabeça fora da visão em primeira pessoa");
            var meshes = body.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).Distinct().ToArray();
            var materials = renderers.Select(renderer => renderer.sharedMaterial).Distinct().ToArray();
            Require(meshes.Length == 2 && body.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3) < 4500 &&
                materials.Length <= 4 && renderers.All(renderer => renderer.sharedMaterial.shader.isSupported),
                "Corpo usa duas malhas, menos de 4500 triângulos e materiais válidos");
            Require(MeshFacesOutward(meshes.First(m=>m.name=="Visitante_FormaArredondada")), "Malha arredondada tem faces externas, visíveis de todos os lados");
            var directional = FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(light => light.isActiveAndEnabled && light.type == LightType.Directional).ToArray();
            Require(directional.Length == 1 && directional[0] == RenderSettings.sun &&
                directional[0].shadows == LightShadows.Soft && directional[0].shadowCustomResolution >= 1024 &&
                QualitySettings.shadows == ShadowQuality.All && QualitySettings.shadowDistance <= 15f,
                "Uma única luz direcional ativa produz sombras suaves próximas ao visitante");

            eye.transform.rotation = Quaternion.Euler(72, 0, 0);
            for (int frame = 0; frame < 30; frame++) yield return null;
            Capture(eye, "body-first-person-feet.png");
            eye.transform.rotation = Quaternion.Euler(90, 0, 0);
            Capture(eye, "body-looking-straight-down.png");
            eye.transform.rotation = Quaternion.Euler(72, 0, 0);
            var diagnostic = new GameObject("BodyDiagnosticCamera").AddComponent<Camera>();
            diagnostic.enabled = false;
            diagnostic.CopyFrom(eye);
            diagnostic.transform.position = player.transform.position + new Vector3(2.4f, 1.7f, 2.6f);
            diagnostic.transform.LookAt(body.position + Vector3.up * .82f);
            Capture(diagnostic, "body-external-shadow.png");
            // Só a captura de diagnóstico revela a parte superior que é sombra na primeira pessoa.
            var shadowOnly = renderers.Where(renderer => renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly).ToArray();
            foreach (var renderer in shadowOnly) renderer.shadowCastingMode = ShadowCastingMode.On;
            Capture(diagnostic, "body-full-diagnostic.png");
            foreach (var renderer in shadowOnly) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            Destroy(diagnostic.gameObject);

            float leftRestY = leftShoe.localPosition.y;
            float maxLift = 0f, maxStride = 0f, minSole = float.PositiveInfinity;
            var leftHand = body.Find("MaoEsquerdo");
            var rightHand = body.Find("MaoDireito");
            var chest = body.Find("Tronco");
            float maxArmStride = 0, maxTorsoAngle = 0;
            int oppositeSwingFrames = 0;
            var cameraRotation = eye.transform.localRotation;
            var audio = FindAnyObjectByType<AmbientAudioManager>();
            Require(audio != null,"Áudio de passos disponível");
            int initialSteps=audio.PlayedFootsteps, contacts=0;
            float lastSide=0;
            bool alternates=true;
            Action<float,Vector3,float> contact=(side,position,speed)=> { if(side==lastSide) alternates=false; lastSide=side; contacts++; };
            visual.FootPlanted+=contact;
            float maxSupportDrift=0;
            int supports=0;
            Vector3 priorLeft=leftShoe.position;
            float priorHeight=leftShoe.localPosition.y;
            for (int frame = 0; frame < 180; frame++)
            {
                controller.Move(new Vector3(0, -.033f, 1.1f / 60f));
                yield return null;
                maxLift = Mathf.Max(maxLift, Mathf.Max(leftShoe.localPosition.y, rightShoe.localPosition.y) - leftRestY);
                maxStride = Mathf.Max(maxStride, Mathf.Abs(leftShoe.localPosition.z - rightShoe.localPosition.z));
                minSole = Mathf.Min(minSole, Mathf.Min(leftShoe.GetComponent<Renderer>().bounds.min.y,
                    rightShoe.GetComponent<Renderer>().bounds.min.y) - controller.bounds.min.y);
                float hands = leftHand.localPosition.z - rightHand.localPosition.z;
                float feet = leftShoe.localPosition.z - rightShoe.localPosition.z;
                maxArmStride = Mathf.Max(maxArmStride, Mathf.Abs(hands));
                maxTorsoAngle = Mathf.Max(maxTorsoAngle, Quaternion.Angle(Quaternion.identity, chest.localRotation));
                if (hands * feet < -.005f) oppositeSwingFrames++;
                if (frame == 30 || frame == 45) Capture(eye, "body-stride-" + frame + ".png");
                if(frame>20 && priorHeight<.057f && leftShoe.localPosition.y<.057f)
                {
                    var drift=leftShoe.position-priorLeft; drift.y=0;
                    maxSupportDrift=Mathf.Max(maxSupportDrift,drift.magnitude); supports++;
                }
                priorLeft=leftShoe.position; priorHeight=leftShoe.localPosition.y;
                if(Environment.GetEnvironmentVariable("MUSEUM_RECORD_WALK")=="1" && frame%2==0)
                    Capture(eye,$"walk-{frame/2:000}.png");
            }
            Require(contacts>=5 && contacts<=7 && alternates && audio.PlayedFootsteps-initialSteps==contacts,
                "Cada contato alternado dispara exatamente um som: "+contacts);
            Require(supports>10 && maxSupportDrift<.006f,
                "Pé apoiado permanece no chão sem patinar: desvio máximo "+maxSupportDrift.ToString("F4")+" m/frame");
            Require(maxLift > .04f && maxStride > .20f && minSole > -.025f,
                "Deslocamento real alterna pernas, levanta pés e preserva solas acima do piso");
            Require(maxArmStride > .2f && maxTorsoAngle > 2f && oppositeSwingFrames > 60,
                "Braços alternam em oposição às pernas e tronco acompanha a passada");
            Require(Quaternion.Angle(cameraRotation, eye.transform.localRotation) < .01f,
                "Animação corporal não balança a câmera");
            for (int frame = 0; frame < 50; frame++) yield return null;
            Require(audio.PlayedFootsteps-initialSteps==contacts,"Parado não produz novos sons");
            Require(Quaternion.Angle(Quaternion.identity, chest.localRotation) < .01f &&
                Mathf.Abs(leftHand.localPosition.z-rightHand.localPosition.z) < .001f,
                "Tronco e braços voltam ao repouso ao parar");
            Require(Mathf.Abs(leftShoe.localPosition.y - leftRestY) < .002f &&
                Mathf.Abs(rightShoe.localPosition.y - leftRestY) < .002f &&
                Mathf.Abs(leftShoe.localPosition.z - rightShoe.localPosition.z) < .002f,
                "Parado, o corpo retorna à postura neutra sem andar no lugar");
            controller.enabled = false;
            player.transform.position += Vector3.forward * 2f;
            controller.enabled = true;
            yield return null;
            Require(audio.PlayedFootsteps-initialSteps==contacts,"Teleporte não produz som de passo");
            Require(Mathf.Abs(leftShoe.localPosition.z - rightShoe.localPosition.z) < .002f,
                "Teleporte não dispara passada longa ou pernas esticadas");

            visual.FootPlanted-=contact;
            visual.enabled = false;
            Require(!body.gameObject.activeSelf, "Desativar o visual também oculta suas malhas e sombras");
            visual.enabled = true;
            Require(body.gameObject.activeSelf, "Reativar o visual restaura o corpo");
            Destroy(visual);
            yield return null;
            yield return null;
            Require(body == null && meshes.All(mesh => mesh == null) && materials.All(material => material == null),
                "Destruir o visual libera corpo, malha e materiais próprios");
            player.gameObject.AddComponent<PlayerBodyVisual>();
            Require(player.transform.Cast<Transform>().Count(child => child.name == "CorpoDoVisitante") == 1,
                "Reconstruir cria somente um corpo");
        }

        private static bool MeshFacesOutward(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude > .00000001f && Vector3.Dot(cross, a + b + c) <= 0f) return false;
            }
            return true;
        }

        private void Capture(Camera cameraView, string filename)
        {
            const int width = 1440, height = 1000;
            string folder = Path.GetFullPath(Environment.GetEnvironmentVariable("MUSEUM_CAPTURE_OUTPUT") ??
                Path.Combine(Application.dataPath, "../../artifacts/bugfix-2026-09-21"));
            Directory.CreateDirectory(folder);
            var previousTarget = cameraView.targetTexture;
            var previousActive = RenderTexture.active;
            float previousAspect = cameraView.aspect, previousFov = cameraView.fieldOfView;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                cameraView.targetTexture = target;
                cameraView.aspect = width / (float)height;
                cameraView.fieldOfView = 62;
                cameraView.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(folder, filename), image.EncodeToPNG());
            }
            finally
            {
                cameraView.targetTexture = previousTarget;
                cameraView.aspect = previousAspect;
                cameraView.fieldOfView = previousFov;
                RenderTexture.active = previousActive;
                Destroy(image);
                target.Release();
                Destroy(target);
            }
            Debug.Log("[BodyValidation] Captura: " + filename);
        }

        private void Require(bool condition, string description)
        {
            if (!condition) throw new Exception("[BodyValidation] " + description);
            checks++;
            Debug.Log("[BodyValidation] PASS " + description);
        }
    }
}
#endif
