using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Relevos originais de baixo custo, gerados no editor e compartilhados no celular.</summary>
    public static class LouvreOrnateFrame
    {
        public static Mesh Create(float width, float height, Vector2[] frameProfile)
        {
            var relief = new Relief();
            // Pérolas espaçadas no toro externo; interrompidas junto aos cantos entalhados.
            foreach (int sign in new[] { -1, 1 })
            {
                int horizontal = Mathf.FloorToInt((width - .76f) / .085f);
                for (int i = 0; i <= horizontal; i++)
                    relief.Bead(new Vector3(Mathf.Lerp(-width / 2 + .38f, width / 2 - .38f,
                        i / (float)horizontal), sign * (height / 2 + .222f), .138f), .012f);
                int vertical = Mathf.FloorToInt((height - .76f) / .085f);
                for (int i = 0; i <= vertical; i++)
                    relief.Bead(new Vector3(sign * (width / 2 + .222f),
                        Mathf.Lerp(-height / 2 + .38f, height / 2 - .38f, i / (float)vertical), .138f), .012f);
            }
            foreach (int sx in new[] { -1, 1 })
                foreach (int sy in new[] { -1, 1 })
                {
                    Vector3 Map(Vector2 p, float z) => new Vector3(sx * (width / 2 + p.x), sy * (height / 2 + p.y), z);
                    // Palmeta em leque sobre o encontro das duas barras, sem invadir a tela.
                    for (int petal = 0; petal < 5; petal++)
                    {
                        float angle = petal * Mathf.PI / 8;
                        var start = new Vector2(.065f, .065f);
                        var end = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (.17f + (petal == 2 ? .025f : 0));
                        relief.Leaf(Map(start, .145f), Map(end, .15f), .030f, .030f);
                    }
                    // Duas volutas simétricas prolongam o canto sobre cada barra.
                    for (int axis = 0; axis < 2; axis++)
                    {
                        Vector2 Swap(Vector2 p) => axis == 0 ? p : new Vector2(p.y, p.x);
                        var path = new List<Vector3>();
                        for (int i = 0; i <= 16; i++)
                        {
                            float t = i / 16f, q = 1 - t;
                            var p = q * q * q * new Vector2(.06f, .085f) +
                                3 * q * q * t * new Vector2(-.11f, .30f) +
                                3 * q * t * t * new Vector2(-.45f, .23f) +
                                t * t * t * new Vector2(-.36f, .095f);
                            path.Add(Map(Swap(p), .147f));
                        }
                        relief.Tube(path, .010f);
                        relief.Bead(path[path.Count - 1], .018f);
                        for (int leaf = 0; leaf < 3; leaf++)
                        {
                            float x = -.085f - leaf * .085f;
                            relief.Leaf(Map(Swap(new Vector2(x, .157f)), .148f),
                                Map(Swap(new Vector2(x - .083f, .235f)), .15f), .025f, .025f);
                        }
                    }
                    relief.Bead(Map(new Vector2(.07f, .07f), .166f), .029f);
                }
            return relief.Finish(width, height, frameProfile);
        }

        private sealed class Relief
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> indices = new List<int>();

            public void Leaf(Vector3 from, Vector3 to, float halfWidth, float height)
            {
                const int lengthSteps = 6, crossSteps = 4;
                int first = vertices.Count;
                var along = (to - from).normalized;
                var across = new Vector3(-along.y, along.x, 0).normalized;
                for (int row = 0; row <= lengthSteps; row++)
                {
                    float t = row / (float)lengthSteps;
                    float envelope = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI)), .8f);
                    float serration = row % 2 == 0 ? .82f : 1;
                    for (int column = 0; column <= crossSteps; column++)
                    {
                        float x = column * 2f / crossSteps - 1;
                        var p = Vector3.Lerp(from, to, t) + across * (x * halfWidth * envelope * serration);
                        // Nervura central alta, bordas onduladas e ponta afilada.
                        p.z += height * envelope * (1 - Mathf.Abs(x) * .84f);
                        vertices.Add(p);
                    }
                }
                for (int row = 0; row < lengthSteps; row++)
                    for (int column = 0; column < crossSteps; column++)
                    {
                        int a = first + row * (crossSteps + 1) + column;
                        Front(a, a + 1, a + crossSteps + 2);
                        Front(a, a + crossSteps + 2, a + crossSteps + 1);
                    }
            }

            public void Bead(Vector3 center, float radius)
            {
                const int sides = 8;
                int first = vertices.Count;
                // Hemisfério com duas faixas; a metade oculta não gasta triângulos.
                for (int ring = 0; ring <= 2; ring++)
                {
                    float a = ring * Mathf.PI / 4;
                    for (int j = 0; j <= sides; j++)
                    {
                        float b = j * Mathf.PI * 2 / sides;
                        vertices.Add(center + new Vector3(Mathf.Cos(b) * Mathf.Cos(a),
                            Mathf.Sin(b) * Mathf.Cos(a), Mathf.Sin(a)) * radius);
                    }
                }
                for (int ring = 0; ring < 2; ring++)
                    for (int j = 0; j < sides; j++)
                    {
                        int a = first + ring * (sides + 1) + j;
                        Front(a, a + 1, a + sides + 2);
                        if (ring == 0) Front(a, a + sides + 2, a + sides + 1);
                    }
            }

            public void Tube(List<Vector3> points, float radius)
            {
                const int sides = 6;
                int first = vertices.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    var tangent = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(0, i - 1)]).normalized;
                    var across = new Vector3(-tangent.y, tangent.x, 0);
                    for (int j = 0; j <= sides; j++)
                    {
                        float a = j * Mathf.PI / sides;
                        vertices.Add(points[i] + radius * (across * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a)));
                    }
                }
                for (int i = 0; i < points.Count - 1; i++)
                    for (int j = 0; j < sides; j++)
                    {
                        int a = first + i * (sides + 1) + j;
                        Front(a, a + 1, a + sides + 2);
                        Front(a, a + sides + 2, a + sides + 1);
                    }
            }

            private void Front(int a, int b, int c)
            {
                if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).z < 0)
                { int swap = b; b = c; c = swap; }
                indices.Add(a); indices.Add(b); indices.Add(c);
            }

            public Mesh Finish(float width, float height, Vector2[] frameProfile)
            {
                // O relevo acompanha o caveto real. Sem esta projeção, as raízes
                // das folhas ficariam suspensas à frente dos rebaixos da madeira.
                for (int i = 0; i < vertices.Count; i++)
                {
                    var vertex = vertices[i];
                    float x = Mathf.Max(0, Mathf.Abs(vertex.x) - width / 2 + .012f);
                    float y = Mathf.Max(0, Mathf.Abs(vertex.y) - height / 2 + .012f);
                    float expansion = Mathf.Sqrt(x * x + y * y) - .012f;
                    float support = frameProfile[0].y;
                    for (int p = 1; p < frameProfile.Length; p++)
                    {
                        if (frameProfile[p].x < frameProfile[p - 1].x) break;
                        support = Mathf.Lerp(frameProfile[p - 1].y, frameProfile[p].y,
                            Mathf.InverseLerp(frameProfile[p - 1].x, frameProfile[p].x, expansion));
                        if (expansion <= frameProfile[p].x) break;
                    }
                    vertex.z += support - .13f;
                    vertices[i] = vertex;
                }
                foreach (var vertex in vertices)
                    if (!float.IsFinite(vertex.x) || !float.IsFinite(vertex.y) || !float.IsFinite(vertex.z))
                        throw new InvalidOperationException("Relevo da moldura contém vértice inválido.");
                var mesh = new Mesh { name = "Ornament_Gala" };
                mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
