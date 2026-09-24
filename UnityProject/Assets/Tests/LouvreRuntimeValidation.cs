#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Checks the saved museum using real colliders and renders its actual geometry.</summary>
    public sealed class LouvreRuntimeValidation : MonoBehaviour
    {
        private readonly List<string> runtimeErrors = new List<string>();
        private Camera eye;
        private PlayerController player;
        private GazeDwellInteraction gaze;

        [Serializable]
        private sealed class GeometryMetrics
        {
            public int activeCameras;
            public int activeRenderers;
            public int renderedMeshInstances;
            public long renderedTriangles;
            public long sharedMeshVertices;
            public int uniqueMeshes;
            public int uniqueMaterials;
            public int activeLights;
            public int shadowCastingLights;
            public int colliders;
            public int exhibitRaycastsPassed;
            public int doorwaysPassed;
            public int enlargedPaintingsPassed;
            public int paintingCornerRaycastsPassed;
            public int paintingNeighbourPairsChecked;
            public float smallestPaintingNeighbourGap;
            public AtmosphereEvidence atmosphere;
            public CirculationEvidence[] circulation;
            public string captureSettings = "Mono Editor camera renders at 1600 x 1000, 62-degree vertical FOV and 4x MSAA; these settings are for visual inspection, not a mobile performance measurement.";
            public SculptureHitEvidence[] sculptureTopHits;
            public PaintingEvidence[] enlargedPaintings;
            public string scope = "Active saved scene geometry. Triangles count only each renderer's assigned submeshes, including its static-batch offset; vertices count once per shared mesh. These are not measured FPS, device draw calls or GPU time.";
        }

        [Serializable]
        private sealed class AtmosphereEvidence
        {
            public int curvedPanels;
            public int islandCases;
            public int planters;
            public int contactShadows;
            public int solidColliders;
            public int addedBenches;
            public DisplayHitEvidence[] displayRaycasts;
        }

        [Serializable]
        private sealed class DisplayHitEvidence
        {
            public string display;
            public string exhibitId;
            public string collider;
            public Vector3 hitPoint;
        }

        [Serializable]
        private sealed class CirculationEvidence
        {
            public string route;
            public Vector3 from;
            public Vector3 to;
            public float clearanceRadius;
        }

        [Serializable]
        private sealed class PaintingEvidence
        {
            public string exhibitId;
            public float canvasWidth;
            public float canvasHeight;
            public float centerHeight;
            public int cornerRaycastsPassed;
            public int frameMaterials;
            public Vector3 frameBoundsCenter;
            public Vector3 frameBoundsSize;
        }

        [Serializable]
        private sealed class SculptureHitEvidence
        {
            public string exhibitId;
            public string collider;
            public Vector3 localSurfaceAim;
            public Vector3 localPhysicsHit;
            public float rayDistance;
        }

        private void Awake() => Application.logMessageReceived += RecordError;
        private void OnDestroy() => Application.logMessageReceived -= RecordError;
        private void RecordError(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                runtimeErrors.Add(message);
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            var routine = Tests();
            while (true)
            {
                object next;
                try
                {
                    if (!routine.MoveNext()) break;
                    next = routine.Current;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    UnityEditor.EditorApplication.Exit(1);
                    yield break;
                }
                yield return next;
            }
            Debug.Log("[LouvreValidation] Scene integration and real scene captures: PASS (mono Editor preview; physical VR remains untested)");
            UnityEditor.EditorApplication.Exit(0);
        }

        private IEnumerator Tests()
        {
            gaze = FindAnyObjectByType<GazeDwellInteraction>();
            Require(gaze != null, "The saved scene has the gaze interaction camera");
            eye = gaze.GetComponent<Camera>();
            player = FindAnyObjectByType<PlayerController>();
            Require(eye != null && player != null, "Player and camera are available");
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled = false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled = false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled = false;
            foreach (var locked in FindObjectsByType<LockPosition>(FindObjectsSortMode.None)) locked.enabled = false;

            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled).ToArray();
            Require(cameras.Length == 1 && cameras[0] == eye,
                "Exactly one active player camera; found " + string.Join(", ", cameras.Select(c => c.name)));
            var architecture = GameObject.Find("Museum_LouvreStyle");
            Require(architecture != null && architecture.GetComponentsInChildren<MeshFilter>().Length > 0,
                "The redesigned architecture is saved and active");

            var exhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Require(exhibits.Length == 28, "28 associated exhibits; found " + exhibits.Length);
            var ids = new HashSet<string>();
            foreach (var exhibit in exhibits)
            {
                Require(exhibit.PaintingData != null && ids.Add(exhibit.PaintingData.uniqueId), "Exhibit data is present and unique: " + exhibit.name);
                var center = exhibit.transform.position;
                var front = exhibit.transform.forward;
                if (exhibit.PaintingData.categoria == "Escultura cenográfica")
                {
                    center = SculptureAim(exhibit);
                    front = Vector3.left;
                }
                if (exhibit.PaintingData.categoria == "Vitrine documental")
                {
                    center += Vector3.up * 1.1f;
                    front = new Vector3(0, .3f, 1).normalized;
                }
                Position(center + front * 2.5f, center);
                Physics.SyncTransforms();
                var found = gaze.RaycastExhibit(new Ray(eye.transform.position, eye.transform.forward));
                Require(found == exhibit, "Unobstructed approach to " + exhibit.name + "; first exhibit: " + (found != null ? found.name : "solid/no hit"));
            }
            Debug.Log("[LouvreValidation] Raycasts to all 28 exhibits: PASS");
            var paintingEvidence = CheckEnlargedPaintings(exhibits, out var neighbourPairs, out var smallestGap);
            var sculptureTopHits = exhibits.Where(exhibit => exhibit.PaintingData.categoria == "Escultura cenográfica")
                .Select(CheckSculptureTop).ToArray();
            Require(sculptureTopHits.Length == 3, "Upper surfaces of all three sculptures were checked");
            Debug.Log("[LouvreValidation] Real surface hits at local sculpture height 1.1 m: PASS");

            var first = exhibits.First(e => e.PaintingData.uniqueId == "homem_amarelo");
            gaze.SetPanelsEnabled(true);
            Position(first.transform.position + first.transform.forward * 2.5f, first.transform.position);
            yield return new WaitForSecondsRealtime(.5f);
            Require(gaze.Focus.Current == first.PaintingData, "Looking at the repositioned exhibition still opens its correct guide");
            gaze.SetPanelsEnabled(false);
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.HidePaintingPanel();
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;

            // Cross each portal at head height and with a pedestrian clearance radius.
            // These short segments avoid treating a sculpture displayed deeper in a room as a blocked doorway.
            Position(new Vector3(0, 1.7f, -16), new Vector3(0, 1.7f, 0));
            Physics.SyncTransforms();
            CheckDoorway("painting gallery", new Vector3(0, 1.7f, 7), new Vector3(0, 1.7f, 11.4f));
            CheckDoorway("sculpture gallery", new Vector3(7, 1.7f, 0), new Vector3(11.4f, 1.7f, 0));
            CheckDoorway("literature gallery", new Vector3(-7, 1.7f, 0), new Vector3(-11.4f, 1.7f, 0));
            CheckDoorway("foyer", new Vector3(0, 1.7f, -7), new Vector3(0, 1.7f, -11.4f));
            foreach (var point in new[]
            {
                new Vector3(0, 1.7f, -8), new Vector3(-7, 1.7f, -6),
                new Vector3(-4.5f, 1.7f, 11.8f), new Vector3(11.5f, 1.7f, -5),
                new Vector3(-11.5f, 1.7f, -5), new Vector3(0, 1.7f, -16)
            })
            {
                var floor = Physics.RaycastAll(point, Vector3.down, 2.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .FirstOrDefault(hit => !BelongsToPlayer(hit.collider) && hit.normal.y > .8f);
                Require(floor.collider != null, "A solid walkable floor supports " + point);
            }
            Debug.Log("[LouvreValidation] Four doorway clearance tests and six floor positions: PASS");

            var atmosphere = CheckAtmosphere(architecture.transform);
            var circulation = CheckGalleryCirculation();

            var metrics = MeasureGeometry();
            metrics.activeCameras = cameras.Length;
            metrics.exhibitRaycastsPassed = exhibits.Length;
            metrics.doorwaysPassed = 4;
            metrics.sculptureTopHits = sculptureTopHits;
            metrics.enlargedPaintingsPassed = paintingEvidence.Length;
            metrics.paintingCornerRaycastsPassed = paintingEvidence.Sum(painting => painting.cornerRaycastsPassed);
            metrics.paintingNeighbourPairsChecked = neighbourPairs;
            metrics.smallestPaintingNeighbourGap = smallestGap;
            metrics.enlargedPaintings = paintingEvidence;
            metrics.atmosphere = atmosphere;
            metrics.circulation = circulation;
            Require(metrics.renderedTriangles <= 350000,
                "The saved scene remains within its 350,000-triangle geometry budget; found " + metrics.renderedTriangles);
            Require(metrics.shadowCastingLights == 0, "The enlarged paintings add no realtime shadow lights");
            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(Path.Combine(OutputDirectory, "louvre-geometry-metrics.json"), JsonUtility.ToJson(metrics, true) + "\n");
            Debug.Log("[LouvreValidation] Geometry: " + metrics.activeRenderers + " renderers, " + metrics.renderedTriangles +
                " triangles, " + metrics.uniqueMaterials + " unique materials, " + metrics.activeLights + " active lights");

            Capture("louvre-hall.png", new Vector3(0, 1.7f, -8), new Vector3(0, 3, 4));
            Capture("louvre-hall-corner.png", new Vector3(-7, 1.7f, -6), new Vector3(3, 3, 4));
            Capture("louvre-painting-gallery.png", new Vector3(-4.5f, 1.7f, 11.8f), new Vector3(3, 2.8f, 22));
            Capture("louvre-real-paintings.png", new Vector3(0, 2, 16.8f), new Vector3(.2f, 2.55f, 23.8f));
            Capture("louvre-sculpture-gallery.png", new Vector3(11.5f, 1.7f, -5), new Vector3(19, 2.5f, 3));
            Capture("louvre-gallery-islands.png", new Vector3(0, 1.7f, 11.5f), new Vector3(0, 1.8f, 19));
            Capture("louvre-literature-gallery.png", new Vector3(-11.6f, 1.7f, -5), new Vector3(-19, 1.8f, 2));
            var island = architecture.GetComponentsInChildren<Transform>()
                .Where(item => item.name.StartsWith("Vitrine_ilha_", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal).First();
            var islandCenter = island.position + Vector3.up;
            Capture("louvre-vitrine-detail.png", islandCenter + new Vector3(1.7f, 1.1f, -2.4f), islandCenter);
            Capture("louvre-ceiling.png", new Vector3(-2, 1.7f, -3), new Vector3(0, 7, 0));
            var frameCenter = first.transform.position - Vector3.up * .10f;
            Capture("louvre-frame-detail.png", frameCenter + first.transform.forward * 2.8f + first.transform.right * .28f, frameCenter);
            var frameCorner = first.transform.position + first.transform.right * 1.1f + Vector3.up * .9f;
            Capture("louvre-frame-corner.png", frameCorner + first.transform.forward * .9f + first.transform.right * .15f, frameCorner);
            yield return null;
            Require(runtimeErrors.Count == 0, "No runtime errors: " + string.Join(" | ", runtimeErrors));
        }

        private AtmosphereEvidence CheckAtmosphere(Transform architecture)
        {
            var atmosphere = architecture.Find("Ambiente_Preenchido");
            Require(atmosphere != null && atmosphere.gameObject.activeInHierarchy,
                "The furnished museum environment is saved and active");
            var objects = atmosphere.GetComponentsInChildren<Transform>();
            var panels = objects.Where(item => item.name.StartsWith("Painel_curvo_", StringComparison.Ordinal)).ToArray();
            var cases = objects.Where(item => item.name.StartsWith("Vitrine_ilha_", StringComparison.Ordinal)).ToArray();
            var planters = objects.Where(item => item.name.StartsWith("Vaso_galeria_", StringComparison.Ordinal)).ToArray();
            var benches = objects.Where(item => item.name.StartsWith("Banco_curvo_Ilha_", StringComparison.Ordinal)).ToArray();
            var shadowRoot = atmosphere.Find("Sombras_de_contato");
            Require(panels.Length == 2 && cases.Length == 4 && planters.Length == 8 && benches.Length == 2,
                "Two curved panels, four document islands, eight planted corners and two curved benches fill the saved museum; " +
                "found " + panels.Length + ", " + cases.Length + ", " + planters.Length + ", " + benches.Length);
            Require(shadowRoot != null, "Contact shadows are saved with the new furnishings");
            var shadows = shadowRoot.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled).ToArray();
            Require(shadows.Length > 0, "Contact shadows ground the new furnishings");
            foreach (var furniture in panels.Concat(cases).Concat(planters).Concat(benches))
            {
                Require(furniture.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled),
                    "Furniture has visible geometry: " + furniture.name);
                Require(furniture.GetComponentsInChildren<Collider>().Any(collider => collider.enabled && !collider.isTrigger),
                    "Furniture prevents walking through its solid base: " + furniture.name);
            }
            Require(!shadowRoot.GetComponentsInChildren<Collider>().Any(collider => collider.enabled),
                "Contact shadows do not add invisible walking obstacles");
            var result = new AtmosphereEvidence
            {
                curvedPanels = panels.Length,
                islandCases = cases.Length,
                planters = planters.Length,
                contactShadows = shadows.Length,
                addedBenches = benches.Length,
                solidColliders = atmosphere.GetComponentsInChildren<Collider>()
                    .Count(collider => collider.enabled && !collider.isTrigger),
                displayRaycasts = CheckDisplayReferences(atmosphere)
            };
            Debug.Log("[LouvreValidation] Furnished environment: " + result.curvedPanels + " panels, " +
                result.islandCases + " island cases, " + result.planters + " planters, " + result.contactShadows +
                " contact shadows and " + result.solidColliders + " solid colliders: PASS");
            return result;
        }

        private DisplayHitEvidence[] CheckDisplayReferences(Transform atmosphere)
        {
            var references = atmosphere.GetComponentsInChildren<ExhibitGuideReference>();
            Require(references.Length == 8, "Both faces of two panels and four vitrines link to existing guides");
            var result = new List<DisplayHitEvidence>();
            foreach (var reference in references)
            {
                Require(reference.Source != null && reference.Source.PaintingData != null,
                    "Display reference has an existing source exhibit: " + reference.name);
                var proxy = reference.GetComponent<BoxCollider>();
                Require(proxy != null && proxy.enabled && !proxy.isTrigger,
                    "Display reference has its own solid gaze surface: " + reference.name);
                bool isVitrine = reference.Source.PaintingData.categoria == "Vitrine documental";
                var aim = reference.transform.TransformPoint(proxy.center);
                var approach = reference.transform.forward;
                if (isVitrine)
                {
                    aim = reference.transform.TransformPoint(proxy.center + Vector3.up * (proxy.size.y * .5f));
                    approach = (Vector3.up + reference.transform.forward * .45f).normalized;
                }
                Position(isVitrine ? aim + reference.transform.forward * 1.8f + Vector3.up * .45f : aim + approach * 1.8f, aim);
                Physics.SyncTransforms();
                var ray = new Ray(eye.transform.position, eye.transform.forward);
                var hit = Physics.RaycastAll(ray, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .Where(candidate => !BelongsToPlayer(candidate.collider)).OrderBy(candidate => candidate.distance).FirstOrDefault();
                Require(hit.collider != null && hit.collider.GetComponentInParent<ExhibitGuideReference>() == reference &&
                    gaze.RaycastExhibit(ray) == reference.Source,
                    "The first physical hit on the new display resolves its original guide: " + reference.name +
                    "; first collider " + (hit.collider != null ? hit.collider.name : "none"));
                result.Add(new DisplayHitEvidence
                {
                    display = reference.name,
                    exhibitId = reference.Source.PaintingData.uniqueId,
                    collider = hit.collider.name,
                    hitPoint = hit.point
                });
            }
            Debug.Log("[LouvreValidation] Eight new display surfaces select existing guides with real physics raycasts: PASS");
            return result.ToArray();
        }

        private CirculationEvidence[] CheckGalleryCirculation()
        {
            var evidence = new List<CirculationEvidence>();
            // The sculpture at (13, 0) is part of the original exhibition. Its route
            // passes between the pedestals instead of casting through the bronze.
            var painting = new[] { new Vector3(0, 0, 11.4f), new Vector3(0, 0, 22) };
            var sculpture = new[]
            {
                new Vector3(11.4f, 0, 0), new Vector3(11.4f, 0, -2),
                new Vector3(21, 0, -2), new Vector3(21, 0, 0)
            };
            var literature = new[] { new Vector3(-11.4f, 0, 0), new Vector3(-22, 0, 0) };
            foreach (float height in new[] { .55f, 1.7f })
            {
                CheckRoute("painting", painting, height, evidence);
                CheckRoute("sculpture", sculpture, height, evidence);
                CheckRoute("literature", literature, height, evidence);
            }
            Debug.Log("[LouvreValidation] Gallery circulation at knee and eye height, 0.30 m radius: PASS (" +
                evidence.Count + " route segments, including endpoint overlaps)");
            return evidence.ToArray();
        }

        private void CheckRoute(string label, Vector3[] path, float height, List<CirculationEvidence> evidence)
        {
            const float radius = .30f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var from = path[i] + Vector3.up * height;
                var to = path[i + 1] + Vector3.up * height;
                var delta = to - from;
                var obstacles = Physics.SphereCastAll(from, radius, delta.normalized, delta.magnitude,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Select(hit => hit.collider)
                    .Concat(Physics.OverlapSphere(from, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    .Concat(Physics.OverlapSphere(to, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    .Where(collider => !BelongsToPlayer(collider)).Distinct().ToArray();
                Require(obstacles.Length == 0, "Continuous circulation through " + label + " at " + height +
                    " m, " + from + " to " + to + ", is blocked by " +
                    string.Join(", ", obstacles.Select(collider => collider.name)));
                evidence.Add(new CirculationEvidence { route = label, from = from, to = to, clearanceRadius = radius });
            }
        }

        private PaintingEvidence[] CheckEnlargedPaintings(PaintingExhibit[] exhibits,
            out int neighbourPairs, out float smallestGap)
        {
            var paintings = exhibits.Where(exhibit =>
                exhibit.PaintingData.categoria != "Escultura cenográfica" &&
                exhibit.PaintingData.categoria != "Vitrine documental").ToArray();
            Require(paintings.Length == 24, "All 24 wall paintings and panels are enlarged");
            var evidence = new List<PaintingEvidence>();
            var frameBounds = new Dictionary<PaintingExhibit, Bounds>();
            foreach (var painting in paintings)
            {
                var scale = painting.transform.lossyScale;
                float width = Mathf.Abs(scale.x), height = Mathf.Abs(scale.y);
                Require(width >= 2.37f && height >= 1.86f,
                    "The enlarged canvas measures at least 2.37 x 1.86 m: " + painting.name + "; " + width + " x " + height);
                var frame = GameObject.Find("Moldura_perfilada_" + painting.PaintingData.uniqueId);
                Require(frame != null, "The enlarged canvas has its own sophisticated frame: " + painting.name);
                Require(Vector3.Distance(frame.transform.position, painting.transform.position) < .005f &&
                    Quaternion.Angle(frame.transform.rotation, painting.transform.rotation) < .1f,
                    "Frame center and plane follow their canvas: " + painting.name);
                var renderers = frame.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled).ToArray();
                Require(renderers.Length > 0, "Frame geometry is active: " + painting.name);
                var materials = renderers.SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().Count();
                Require(materials <= 3, "At most three shared materials dress each frame: " + painting.name + "; found " + materials);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                Require(Finite(bounds.center) && Finite(bounds.size), "Frame bounds contain only finite values: " + painting.name);
                frameBounds.Add(painting, bounds);

                int cornerHits = 0;
                // Four rays cover the newly expanded interaction area. Their targets are
                // 90% of the way to each canvas edge, clear of the real frame opening.
                foreach (int x in new[] { -1, 1 })
                    foreach (int y in new[] { -1, 1 })
                    {
                        var target = painting.transform.TransformPoint(new Vector3(x * .45f, y * .45f, 0));
                        Position(target + painting.transform.forward * 2.5f, target);
                        Physics.SyncTransforms();
                        var selected = gaze.RaycastExhibit(new Ray(eye.transform.position, eye.transform.forward));
                        Require(selected == painting, "Expanded canvas corner selects its own guide: " + painting.name +
                            " (" + x + ", " + y + "); found " + (selected != null ? selected.name : "solid/no hit"));
                        cornerHits++;
                    }
                evidence.Add(new PaintingEvidence
                {
                    exhibitId = painting.PaintingData.uniqueId,
                    canvasWidth = width,
                    canvasHeight = height,
                    centerHeight = painting.transform.position.y,
                    cornerRaycastsPassed = cornerHits,
                    frameMaterials = materials,
                    frameBoundsCenter = bounds.center,
                    frameBoundsSize = bounds.size
                });
            }

            neighbourPairs = 0;
            smallestGap = float.PositiveInfinity;
            for (int i = 0; i < paintings.Length; i++)
                for (int j = i + 1; j < paintings.Length; j++)
                {
                    var a = paintings[i];
                    var b = paintings[j];
                    var delta = b.transform.position - a.transform.position;
                    if (Vector3.Dot(a.transform.forward, b.transform.forward) < .999f ||
                        Mathf.Abs(Vector3.Dot(delta, a.transform.forward)) > .1f) continue;
                    float gap = Mathf.Abs(Vector3.Dot(frameBounds[b].center - frameBounds[a].center, a.transform.right)) -
                        BoundsRadius(frameBounds[a], a.transform.right) - BoundsRadius(frameBounds[b], a.transform.right);
                    Require(gap >= .2f, "Frames on the same wall retain 20 cm of separation: " + a.name + " / " + b.name +
                        "; gap " + gap.ToString("F3") + " m");
                    smallestGap = Mathf.Min(smallestGap, gap);
                    neighbourPairs++;
                }
            Require(neighbourPairs > 0, "Neighbouring wall frames were checked for overlap");
            Debug.Log("[LouvreValidation] 24 enlarged canvases, 96 corner raycasts, " + neighbourPairs +
                " wall-frame pairs: PASS; smallest frame gap " + smallestGap.ToString("F3") + " m");
            return evidence.ToArray();
        }

        private static float BoundsRadius(Bounds bounds, Vector3 axis) =>
            Mathf.Abs(axis.x) * bounds.extents.x + Mathf.Abs(axis.y) * bounds.extents.y + Mathf.Abs(axis.z) * bounds.extents.z;

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private void CheckDoorway(string label, Vector3 from, Vector3 to)
        {
            var delta = to - from;
            var hits = Physics.SphereCastAll(from, .25f, delta.normalized, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Where(hit => !BelongsToPlayer(hit.collider)).ToArray();
            Require(hits.Length == 0, "Doorway to " + label + " is blocked by " + string.Join(", ", hits.Select(hit => hit.collider.name)));
        }

        private bool BelongsToPlayer(Collider collider) => collider != null && collider.transform.IsChildOf(player.transform);

        public static Vector3 SculptureAim(PaintingExhibit exhibit)
        {
            var collider = exhibit.GetComponentsInChildren<MeshCollider>().FirstOrDefault(c => c.enabled && c.sharedMesh != null);
            if (collider == null) return exhibit.transform.position + Vector3.up * .5f;
            var vertices = collider.sharedMesh.vertices;
            var triangles = collider.sharedMesh.triangles;
            float best = float.PositiveInfinity;
            Vector3 target = Vector3.zero;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a).normalized;
                if (normal.x > -.2f) continue;
                var point = (a + b + c) / 3;
                float score = Mathf.Abs(point.y - .5f) * 100 + point.x;
                if (score < best) { best = score; target = point; }
            }
            Require(!float.IsInfinity(best), "A left-facing sculpture surface is available");
            return collider.transform.TransformPoint(target);
        }

        private SculptureHitEvidence CheckSculptureTop(PaintingExhibit exhibit)
        {
            var proxy = exhibit.GetComponentsInChildren<MeshCollider>()
                .FirstOrDefault(collider => collider.enabled && !collider.isTrigger && collider.sharedMesh != null);
            Require(proxy != null, "Sculpture has an active mesh collider: " + exhibit.name);
            const float localHeight = 1.1f;
            var mesh = proxy.sharedMesh;
            Require(mesh.bounds.min.y < localHeight && mesh.bounds.max.y > localHeight,
                "The sculpture surface reaches the upper test section: " + exhibit.name);

            // A ray aimed at (0, 1.1, 0) can miss a curved bronze. Intersect the actual
            // mesh triangles with that height and aim at the leftmost section midpoint.
            // This selects an actual face point, not a guessed point inside its AABB.
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var section = new List<Vector3>(3);
            var surface = new Vector3(float.PositiveInfinity, localHeight, 0);
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                section.Clear();
                for (int edge = 0; edge < 3; edge++)
                {
                    var a = vertices[triangles[triangle + edge]];
                    var b = vertices[triangles[triangle + (edge + 1) % 3]];
                    if ((a.y <= localHeight && b.y > localHeight) || (b.y <= localHeight && a.y > localHeight))
                        section.Add(Vector3.Lerp(a, b, (localHeight - a.y) / (b.y - a.y)));
                }
                if (section.Count != 2) continue;
                var midpoint = (section[0] + section[1]) * .5f;
                if (midpoint.x < surface.x) surface = midpoint;
            }
            Require(!float.IsInfinity(surface.x), "A real upper triangle section exists: " + exhibit.name);
            var target = proxy.transform.TransformPoint(surface);
            var approach = proxy.transform.TransformDirection(Vector3.left).normalized;
            Position(target + approach * 2.5f, target);
            Physics.SyncTransforms();
            var ray = new Ray(eye.transform.position, eye.transform.forward);
            var hit = Physics.RaycastAll(ray, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                .Where(candidate => !BelongsToPlayer(candidate.collider)).OrderBy(candidate => candidate.distance).FirstOrDefault();
            Require(hit.collider == proxy && gaze.RaycastExhibit(ray) == exhibit,
                "Upper bronze surface selects its own guide: " + exhibit.name + "; hit " + (hit.collider != null ? hit.collider.name : "none"));
            var localHit = proxy.transform.InverseTransformPoint(hit.point);
            Require(Mathf.Abs(localHit.y - localHeight) < .005f, "The physics hit is on the tested upper section");
            Debug.Log("[LouvreValidation] Sculpture upper hit: " + exhibit.PaintingData.uniqueId + " at " + localHit.ToString("F4"));
            return new SculptureHitEvidence
            {
                exhibitId = exhibit.PaintingData.uniqueId,
                collider = hit.collider.name,
                localSurfaceAim = surface,
                localPhysicsHit = localHit,
                rayDistance = hit.distance
            };
        }

        private static GeometryMetrics MeasureGeometry()
        {
            var result = new GeometryMetrics();
            var meshes = new HashSet<Mesh>();
            var materials = new HashSet<Material>();
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(renderer => renderer.enabled).ToArray();
            result.activeRenderers = renderers.Length;
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials) if (material != null) materials.Add(material);
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : (renderer as SkinnedMeshRenderer)?.sharedMesh;
                if (mesh == null) continue;
                if (meshes.Add(mesh)) result.sharedMeshVertices += mesh.vertexCount;
                result.renderedMeshInstances++;
                // Static batching shares a combined mesh across many renderers. Each renderer
                // owns a slice starting at subMeshStartIndex, not the entire combined mesh.
                int firstSubmesh = renderer is MeshRenderer meshRenderer ? meshRenderer.subMeshStartIndex : 0;
                int endSubmesh = Mathf.Min(mesh.subMeshCount, firstSubmesh + renderer.sharedMaterials.Length);
                for (int submesh = firstSubmesh; submesh < endSubmesh; submesh++)
                    if (mesh.GetTopology(submesh) == UnityEngine.MeshTopology.Triangles)
                        result.renderedTriangles += mesh.GetIndexCount(submesh) / 3;
            }
            result.uniqueMeshes = meshes.Count;
            result.uniqueMaterials = materials.Count;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(light => light.enabled).ToArray();
            result.activeLights = lights.Length;
            result.shadowCastingLights = lights.Count(light => light.shadows != LightShadows.None);
            result.colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None).Count(collider => collider.enabled && !collider.isTrigger);
            return result;
        }

        private void Position(Vector3 position, Vector3 target)
        {
            player.transform.position += position - eye.transform.position;
            eye.transform.rotation = Quaternion.LookRotation(target - position);
        }

        private static string OutputDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts"));

        private void Capture(string filename, Vector3 position, Vector3 target)
        {
            const int width = 1600, height = 1000;
            Position(position, target);
            var previousTarget = eye.targetTexture;
            var previousActive = RenderTexture.active;
            var previousAspect = eye.aspect;
            var previousFov = eye.fieldOfView;
            var render = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                eye.targetTexture = render;
                eye.aspect = width / (float)height;
                eye.fieldOfView = 62;
                eye.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(OutputDirectory, filename), texture.EncodeToPNG());
            }
            finally
            {
                eye.targetTexture = previousTarget;
                eye.aspect = previousAspect;
                eye.fieldOfView = previousFov;
                RenderTexture.active = previousActive;
                Destroy(texture);
                render.Release();
                Destroy(render);
            }
            Debug.Log("[LouvreValidation] Actual scene capture: " + filename);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("[LouvreValidation] " + message);
        }
    }
}
#endif
