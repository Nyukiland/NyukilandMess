using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SphericalPolyhedronVisualizer : MonoBehaviour
{
    [Header("Polyhedron Settings")]
    [Range(4, 120)]
    [SerializeField]
    private int _faceCount = 14;

    [Min(0.1f)]
    [SerializeField]
    private float _radius = 1.5f;

    [Range(1, 30)]
    [SerializeField]
    private int _relaxationIterations = 15;

    [Header("Playback")]
    [SerializeField]
    private bool _playOnStart = true;

    [Header("Phase Durations (Seconds)")]
    [Min(0f)]
    [SerializeField]
    private float _sphereAppearDuration = 0.35f;

    [Min(0f)]
    [SerializeField]
    private float _placeDotsDuration = 0.50f;

    [Min(0f)]
    [SerializeField]
    private float _sphereTransparentDuration = 0.30f;

    [Min(0f)]
    [SerializeField]
    private float _drawTrianglesDuration = 0.50f;

    [Min(0f)]
    [SerializeField]
    private float _showCentersDuration = 0.35f;

    [Min(0f)]
    [SerializeField]
    private float _drawFacesDuration = 0.45f;

    [Min(0f)]
    [SerializeField]
    private float _hideGuideDuration = 0.20f;

    [Min(0f)]
    [SerializeField]
    private float _relaxationDuration = 0.40f;

    [Min(0f)]
    [SerializeField]
    private float _finalRevealDuration = 0.20f;

    [Header("Visual Styling")]
    [SerializeField]
    private Color _sphereColor = new(0.16f, 0.22f, 0.32f, 1f);

    [Range(0.05f, 0.5f)]
    [SerializeField]
    private float _transparentSphereAlpha = 0.16f;

    [SerializeField]
    private Color _hullEdgeColor = new(0.85f, 0.92f, 1f, 0.85f);

    [SerializeField]
    private Color _circumcenterColor = Color.white;

    private struct DualSnapshot
    {
        public Vector3[] Seeds;
        public List<int> HullTris;
        public List<(int a, int b)> HullEdges;
        public Vector3[] DualVerts;
        public List<int>[] SeedRings;
    }

    private GameObject _sphereObj;
    private Material _sphereMat;
    private Mesh _sphereMesh;

    private Mesh _dotsMesh;
    private Material _vertexColorUnlitMat;

    private Mesh _hullLinesMesh;
    private Mesh _faceLinesMesh;

    private GameObject _finalPolyObj;
    private Mesh _finalPolyMesh;
    private Material _finalPolyMat;
    private Texture2D _paletteTex;

    private Color[] _seedColors;
    private List<DualSnapshot> _relaxationSteps;
    private Coroutine _animRoutine;

    private readonly List<Vector3> _vertsBuf = new(2048);
    private readonly List<Color> _colorsBuf = new(2048);
    private readonly List<int> _trisBuf = new(4096);

    public float TotalDuration =>
        _sphereAppearDuration +
        _placeDotsDuration +
        _sphereTransparentDuration +
        _drawTrianglesDuration +
        _showCentersDuration +
        _drawFacesDuration +
        _hideGuideDuration +
        _relaxationDuration +
        _finalRevealDuration;

    private void Awake()
    {
        InitializeObjects();
    }

    private void Start()
    {
        if (_playOnStart)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        DestroyAsset(_sphereMesh);
        DestroyAsset(_sphereMat);
        DestroyAsset(_dotsMesh);
        DestroyAsset(_hullLinesMesh);
        DestroyAsset(_faceLinesMesh);
        DestroyAsset(_vertexColorUnlitMat);
        DestroyAsset(_finalPolyMesh);
        DestroyAsset(_finalPolyMat);
        DestroyAsset(_paletteTex);
    }

    [ContextMenu("Replay Animation")]
    public void Play()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
        }

        PrecomputeSnapshots();
        _animRoutine = StartCoroutine(AnimateRoutine());
    }

    private void InitializeObjects()
    {
        _vertexColorUnlitMat = new(Shader.Find("Sprites/Default"));

        _sphereObj = new("GuideSphere");
        _sphereObj.transform.SetParent(transform, false);
        _sphereMesh = BuildUvSphereMesh(32, 20);
        _sphereObj.AddComponent<MeshFilter>().sharedMesh = _sphereMesh;

        Shader litShader = GetLitShader();
        _sphereMat = new(litShader);
        SetMaterialTransparent(_sphereMat, false);
        _sphereMat.color = _sphereColor;
        _sphereObj.AddComponent<MeshRenderer>().sharedMaterial = _sphereMat;

        _dotsMesh = new()
        {
            name = "VisualizerDots"
        };
        _dotsMesh.MarkDynamic();
        CreateChildMeshObject("DotsLayer", _dotsMesh, _vertexColorUnlitMat);

        _hullLinesMesh = new()
        {
            name = "HullLines"
        };
        _hullLinesMesh.MarkDynamic();
        CreateChildMeshObject("HullLinesLayer", _hullLinesMesh, _vertexColorUnlitMat);

        _faceLinesMesh = new()
        {
            name = "FaceLines"
        };
        _faceLinesMesh.MarkDynamic();
        CreateChildMeshObject("FaceLinesLayer", _faceLinesMesh, _vertexColorUnlitMat);

        _finalPolyObj = new("FinalPolyhedron");
        _finalPolyObj.transform.SetParent(transform, false);
        _finalPolyMesh = new()
        {
            name = "FinalPolyhedronMesh"
        };
        _finalPolyObj.AddComponent<MeshFilter>().sharedMesh = _finalPolyMesh;
        _finalPolyMat = new(litShader);
        _finalPolyMat.color = Color.white;
        _finalPolyObj.AddComponent<MeshRenderer>().sharedMaterial = _finalPolyMat;
    }

    private void CreateChildMeshObject(string objName, Mesh mesh, Material mat)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private void PrecomputeSnapshots()
    {
        int n = Mathf.Clamp(_faceCount, 4, 120);

        _seedColors = new Color[n];
        for (int i = 0; i < n; i++)
        {
            float hue = (i * 0.61803398875f) % 1f;
            _seedColors[i] = Color.HSVToRGB(hue, 0.78f, 0.96f);
        }

        DestroyAsset(_paletteTex);
        _paletteTex = new(n, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _paletteTex.SetPixels(_seedColors);
        _paletteTex.Apply();
        _finalPolyMat.mainTexture = _paletteTex;

        _relaxationSteps = new(_relaxationIterations + 1);
        Vector3[] seeds = GenerateFibonacciPoints(n);
        DualSnapshot current = BuildDualSnapshot(seeds, _radius);
        _relaxationSteps.Add(current);

        for (int iter = 0; iter < _relaxationIterations; iter++)
        {
            seeds = RelaxSeedsStep(current);
            current = BuildDualSnapshot(seeds, _radius);
            _relaxationSteps.Add(current);
        }

        BuildFinalSolidMesh(_finalPolyMesh, current, _seedColors.Length, _radius);
    }

    private IEnumerator AnimateRoutine()
    {
        DualSnapshot initial = _relaxationSteps[0];
        int n = initial.Seeds.Length;

        float dotRadius = _radius * Mathf.Clamp(0.32f / Mathf.Sqrt(n), 0.022f, 0.075f);
        float centerDotRadius = dotRadius * 0.68f;
        float hullThickness = dotRadius * 0.28f;
        float faceThickness = dotRadius * 0.42f;

        // Cumulative timestamps (in seconds) for each phase boundary
        float t1SphereEnd = _sphereAppearDuration;
        float t2DotsEnd = t1SphereEnd + _placeDotsDuration;
        float t3TransparentEnd = t2DotsEnd + _sphereTransparentDuration;
        float t4TrianglesEnd = t3TransparentEnd + _drawTrianglesDuration;
        float t5CentersEnd = t4TrianglesEnd + _showCentersDuration;
        float t6FacesEnd = t5CentersEnd + _drawFacesDuration;
        float t7HideGuideEnd = t6FacesEnd + _hideGuideDuration;
        float t8RelaxEnd = t7HideGuideEnd + _relaxationDuration;
        float t9RevealEnd = t8RelaxEnd + _finalRevealDuration;

        _sphereObj.SetActive(true);
        _sphereObj.transform.localScale = Vector3.zero;
        SetMaterialTransparent(_sphereMat, false);
        _sphereMat.color = _sphereColor;

        _finalPolyObj.SetActive(false);
        _dotsMesh.Clear();
        _hullLinesMesh.Clear();
        _faceLinesMesh.Clear();

        float elapsed = 0f;
        while (elapsed < t9RevealEnd)
        {
            elapsed += Time.deltaTime;

            // Phase 1: Sphere scale in
            if (elapsed < t1SphereEnd)
            {
                float p = Smooth01(PhaseProgress(elapsed, 0f, _sphereAppearDuration));
                _sphereObj.transform.localScale = Vector3.one * (_radius * p);
            }
            else
            {
                _sphereObj.transform.localScale = Vector3.one * _radius;
            }

            // Phase 3 & Phase 7: Sphere transparency & fade out
            if (elapsed >= t2DotsEnd && elapsed < t6FacesEnd)
            {
                float p = Smooth01(PhaseProgress(elapsed, t2DotsEnd, _sphereTransparentDuration));
                SetMaterialTransparent(_sphereMat, true);
                Color c = _sphereColor;
                c.a = Mathf.Lerp(1f, _transparentSphereAlpha, p);
                _sphereMat.color = c;
            }
            else if (elapsed >= t6FacesEnd && elapsed < t7HideGuideEnd)
            {
                float p = Smooth01(PhaseProgress(elapsed, t6FacesEnd, _hideGuideDuration));
                SetMaterialTransparent(_sphereMat, true);
                Color c = _sphereColor;
                c.a = Mathf.Lerp(_transparentSphereAlpha, 0f, p);
                _sphereMat.color = c;
            }
            else if (elapsed >= t7HideGuideEnd)
            {
                _sphereObj.SetActive(false);
            }

            // Phase 8: Progressive Lloyd relaxation
            DualSnapshot activeSnap = initial;
            if (elapsed >= t7HideGuideEnd)
            {
                float relaxT = Smooth01(PhaseProgress(elapsed, t7HideGuideEnd, _relaxationDuration));
                float scaledStep = relaxT * (_relaxationSteps.Count - 1);
                int idx0 = Mathf.FloorToInt(scaledStep);
                int idx1 = Mathf.Min(idx0 + 1, _relaxationSteps.Count - 1);
                float subT = scaledStep - idx0;

                if (idx0 == idx1 || subT < 1e-4f)
                {
                    activeSnap = _relaxationSteps[idx0];
                }
                else
                {
                    Vector3[] interpSeeds = new Vector3[n];
                    for (int i = 0; i < n; i++)
                    {
                        interpSeeds[i] = Vector3.Slerp(_relaxationSteps[idx0].Seeds[i], _relaxationSteps[idx1].Seeds[i], subT).normalized;
                    }

                    activeSnap = BuildDualSnapshot(interpSeeds, _radius);
                }
            }

            // Phase 9: Final reveal & wireframe fade out
            float revealProgress = PhaseProgress(elapsed, t8RelaxEnd, _finalRevealDuration);
            float overlayAlpha = elapsed < t8RelaxEnd ? 1f : 1f - Smooth01(revealProgress);

            // Phase 2 & Phase 5: Seed dots + Triangle circumcenter dots
            ClearBuffers();
            if (elapsed >= t1SphereEnd && overlayAlpha > 0.001f)
            {
                float seedProgress = PhaseProgress(elapsed, t1SphereEnd, _placeDotsDuration);
                int visibleSeeds = Mathf.CeilToInt(seedProgress * n);

                for (int i = 0; i < visibleSeeds; i++)
                {
                    float localPop = _placeDotsDuration <= 1e-5f ? 1f : Mathf.Clamp01((seedProgress * n - i) * 2.5f);
                    float scale = dotRadius * EaseOutBack(localPop) * overlayAlpha;
                    Color c = _seedColors[i];
                    c.a = overlayAlpha;
                    AppendOctahedron(_vertsBuf, _colorsBuf, _trisBuf, activeSnap.Seeds[i] * (_radius * 1.005f), scale, c);
                }
            }

            if (elapsed >= t4TrianglesEnd && overlayAlpha > 0.001f)
            {
                float centerProgress = PhaseProgress(elapsed, t4TrianglesEnd, _showCentersDuration);
                int totalCenters = activeSnap.DualVerts.Length;
                int visibleCenters = Mathf.CeilToInt(centerProgress * totalCenters);

                for (int i = 0; i < visibleCenters; i++)
                {
                    float localPop = _showCentersDuration <= 1e-5f ? 1f : Mathf.Clamp01((centerProgress * totalCenters - i) * 2.5f);
                    float scale = centerDotRadius * EaseOutBack(localPop) * overlayAlpha;
                    Color c = _circumcenterColor;
                    c.a = overlayAlpha;
                    AppendOctahedron(_vertsBuf, _colorsBuf, _trisBuf, activeSnap.DualVerts[i] * 1.006f, scale, c);
                }
            }
            ApplyBuffersToMesh(_dotsMesh);

            // Phase 4 & Phase 7: Hull triangles draw & disappear
            ClearBuffers();
            if (elapsed >= t3TransparentEnd && elapsed < t7HideGuideEnd)
            {
                float drawProgress = PhaseProgress(elapsed, t3TransparentEnd, _drawTrianglesDuration);
                float fadeOut = elapsed < t6FacesEnd
                    ? 1f
                    : 1f - Smooth01(PhaseProgress(elapsed, t6FacesEnd, _hideGuideDuration));

                int totalEdges = activeSnap.HullEdges.Count;
                float edgeCursor = drawProgress * totalEdges;
                int fullEdges = Mathf.Min(totalEdges, Mathf.CeilToInt(edgeCursor));

                Color c = _hullEdgeColor;
                c.a *= fadeOut;
                float thick = hullThickness * fadeOut;

                for (int e = 0; e < fullEdges; e++)
                {
                    float segT = _drawTrianglesDuration <= 1e-5f ? 1f : Mathf.Clamp01(edgeCursor - e);
                    var (ia, ib) = activeSnap.HullEdges[e];
                    Vector3 p0 = activeSnap.Seeds[ia] * (_radius * 1.002f);
                    Vector3 p1 = Vector3.Slerp(activeSnap.Seeds[ia], activeSnap.Seeds[ib], segT).normalized * (_radius * 1.002f);
                    AppendEdgePrism(_vertsBuf, _colorsBuf, _trisBuf, p0, p1, thick, c);
                }
            }
            ApplyBuffersToMesh(_hullLinesMesh);

            // Phase 6: Colored dual face loops draw
            ClearBuffers();
            if (elapsed >= t5CentersEnd && overlayAlpha > 0.001f)
            {
                float faceDrawProgress = PhaseProgress(elapsed, t5CentersEnd, _drawFacesDuration);
                float faceCursor = faceDrawProgress * n;
                int visibleFaces = Mathf.Min(n, Mathf.CeilToInt(faceCursor));

                for (int s = 0; s < visibleFaces; s++)
                {
                    List<int> ring = activeSnap.SeedRings[s];
                    if (ring == null || ring.Count < 3)
                    {
                        continue;
                    }

                    float faceSegProgress = _drawFacesDuration <= 1e-5f ? 1f : Mathf.Clamp01(faceCursor - s);
                    float totalSegs = ring.Count;
                    float segCursor = faceSegProgress * totalSegs;
                    int segCount = Mathf.Min(ring.Count, Mathf.CeilToInt(segCursor));

                    Color c = _seedColors[s];
                    c.a = overlayAlpha;

                    for (int k = 0; k < segCount; k++)
                    {
                        float localSegT = _drawFacesDuration <= 1e-5f ? 1f : Mathf.Clamp01(segCursor - k);
                        Vector3 v0 = activeSnap.DualVerts[ring[k]] * 1.008f;
                        Vector3 v1 = activeSnap.DualVerts[ring[(k + 1) % ring.Count]] * 1.008f;
                        Vector3 p1 = Vector3.Lerp(v0, v1, localSegT);
                        AppendEdgePrism(_vertsBuf, _colorsBuf, _trisBuf, v0, p1, faceThickness * overlayAlpha, c);
                    }
                }
            }
            ApplyBuffersToMesh(_faceLinesMesh);

            // Phase 9: Final solid polyhedron scale-in
            if (elapsed >= t8RelaxEnd)
            {
                _finalPolyObj.SetActive(true);
                float p = Smooth01(revealProgress);
                _finalPolyObj.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, p);
            }

            yield return null;
        }

        _sphereObj.SetActive(false);
        _dotsMesh.Clear();
        _hullLinesMesh.Clear();
        _faceLinesMesh.Clear();
        _finalPolyObj.SetActive(true);
        _finalPolyObj.transform.localScale = Vector3.one;
        _animRoutine = null;
    }

    private static float PhaseProgress(float elapsed, float startTime, float duration)
    {
        if (duration <= 1e-5f)
        {
            return elapsed >= startTime ? 1f : 0f;
        }

        return Mathf.Clamp01((elapsed - startTime) / duration);
    }

    private static DualSnapshot BuildDualSnapshot(Vector3[] seeds, float sphereRadius)
    {
        int n = seeds.Length;
        List<int> hullTris = ComputeConvexHull(seeds);
        int triCount = hullTris.Count / 3;

        Vector3[] dualVerts = new Vector3[triCount];
        List<int>[] seedRings = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            seedRings[i] = new(6);
        }

        HashSet<long> edgeSet = new();
        List<(int a, int b)> hullEdges = new(triCount * 3 / 2);

        for (int t = 0; t < triCount; t++)
        {
            int ia = hullTris[t * 3];
            int ib = hullTris[t * 3 + 1];
            int ic = hullTris[t * 3 + 2];

            Vector3 a = seeds[ia], b = seeds[ib], c = seeds[ic];
            Vector3 cc = Vector3.Cross(b - a, c - a);
            if (cc.sqrMagnitude < 1e-12f)
            {
                cc = a + b + c;
            }

            cc.Normalize();
            if (Vector3.Dot(cc, a + b + c) < 0f)
            {
                cc = -cc;
            }

            dualVerts[t] = cc * sphereRadius;

            seedRings[ia].Add(t);
            seedRings[ib].Add(t);
            seedRings[ic].Add(t);

            AddUndirectedEdge(ia, ib, edgeSet, hullEdges);
            AddUndirectedEdge(ib, ic, edgeSet, hullEdges);
            AddUndirectedEdge(ic, ia, edgeSet, hullEdges);
        }

        for (int s = 0; s < n; s++)
        {
            if (seedRings[s].Count >= 3)
            {
                SortRingClockwise(seedRings[s], dualVerts, seeds[s]);
            }
        }

        return new()
        {
            Seeds = seeds,
            HullTris = hullTris,
            HullEdges = hullEdges,
            DualVerts = dualVerts,
            SeedRings = seedRings
        };
    }

    private static Vector3[] RelaxSeedsStep(DualSnapshot snap)
    {
        int n = snap.Seeds.Length;
        Vector3[] nextSeeds = new Vector3[n];

        for (int s = 0; s < n; s++)
        {
            List<int> ring = snap.SeedRings[s];
            if (ring == null || ring.Count < 3)
            {
                nextSeeds[s] = snap.Seeds[s];
                continue;
            }

            Vector3 weightedSum = Vector3.zero;
            float totalArea = 0f;
            Vector3 center = snap.Seeds[s];

            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 v0 = snap.DualVerts[ring[i]].normalized;
                Vector3 v1 = snap.DualVerts[ring[(i + 1) % ring.Count]].normalized;

                float area = Vector3.Cross(v0 - center, v1 - center).magnitude * 0.5f;
                Vector3 triCentroid = (center + v0 + v1) / 3f;

                weightedSum += triCentroid * area;
                totalArea += area;
            }

            nextSeeds[s] = totalArea > 1e-8f ? (weightedSum / totalArea).normalized : snap.Seeds[s];
        }

        return nextSeeds;
    }

    private static void BuildFinalSolidMesh(Mesh mesh, DualSnapshot snap, int colorCount, float sphereRadius)
    {
        mesh.Clear();
        int n = snap.Seeds.Length;

        List<Vector3> verts = new(snap.DualVerts.Length * 3);
        List<Vector3> normals = new(snap.DualVerts.Length * 3);
        List<Vector2> uvs = new(snap.DualVerts.Length * 3);
        List<int> tris = new(snap.DualVerts.Length * 3);

        for (int s = 0; s < n; s++)
        {
            List<int> ring = snap.SeedRings[s];
            if (ring == null || ring.Count < 3)
            {
                continue;
            }

            Vector3 faceNormal = snap.Seeds[s];
            Vector2 paletteUv = new((s + 0.5f) / colorCount, 0.5f);

            int baseIdx = verts.Count;
            for (int i = 0; i < ring.Count; i++)
            {
                verts.Add(snap.DualVerts[ring[i]]);
                normals.Add(faceNormal);
                uvs.Add(paletteUv);
            }

            for (int i = 1; i < ring.Count - 1; i++)
            {
                Vector3 cross = Vector3.Cross(verts[baseIdx + i] - verts[baseIdx], verts[baseIdx + i + 1] - verts[baseIdx]);
                if (Vector3.Dot(cross, faceNormal) >= 0f)
                {
                    tris.Add(baseIdx);
                    tris.Add(baseIdx + i);
                    tris.Add(baseIdx + i + 1);
                }
                else
                {
                    tris.Add(baseIdx);
                    tris.Add(baseIdx + i + 1);
                    tris.Add(baseIdx + i);
                }
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }

    private static Vector3[] GenerateFibonacciPoints(int count)
    {
        Vector3[] points = new Vector3[count];
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < count; i++)
        {
            float y = 1f - (i / (float)(count - 1)) * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float theta = goldenAngle * i;
            points[i] = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r).normalized;
        }
        return points;
    }

    private static void SortRingClockwise(List<int> ring, Vector3[] positions, Vector3 normal)
    {
        Vector3 tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        ring.Sort((idxA, idxB) =>
        {
            Vector3 va = positions[idxA];
            Vector3 vb = positions[idxB];
            float angA = Mathf.Atan2(Vector3.Dot(va, bitangent), Vector3.Dot(va, tangent));
            float angB = Mathf.Atan2(Vector3.Dot(vb, bitangent), Vector3.Dot(vb, tangent));
            return angA.CompareTo(angB);
        });
    }

    private static void AddUndirectedEdge(int a, int b, HashSet<long> set, List<(int a, int b)> list)
    {
        int min = a < b ? a : b;
        int max = a < b ? b : a;
        long key = ((long)min << 32) | (uint)max;
        if (set.Add(key))
        {
            list.Add((min, max));
        }
    }

    private struct HullFace
    {
        public int A, B, C;
        public Vector3 Normal;
        public float Offset;

        public HullFace(int a, int b, int c, Vector3 interiorPoint, Vector3[] pts)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            Normal = Vector3.Cross(pts[b] - pts[a], pts[c] - pts[a]).normalized;
            if (Vector3.Dot(Normal, interiorPoint - pts[a]) > 0f)
            {
                (this.B, this.C) = (this.C, this.B);
                Normal = -Normal;
            }
            Offset = Vector3.Dot(Normal, pts[a]);
        }
    }

    private static List<int> ComputeConvexHull(Vector3[] pts)
    {
        int n = pts.Length;
        List<int> result = new();
        if (n < 4)
        {
            return result;
        }

        int p0 = 0, p1 = 1;
        float maxDistSq = 0f;
        for (int i = 1; i < n; i++)
        {
            float d = (pts[i] - pts[p0]).sqrMagnitude;
            if (d > maxDistSq) { maxDistSq = d; p1 = i; }
        }

        int p2 = -1;
        float maxAreaSq = 0f;
        Vector3 dir01 = pts[p1] - pts[p0];
        for (int i = 0; i < n; i++)
        {
            if (i == p0 || i == p1)
            {
                continue;
            }

            float areaSq = Vector3.Cross(dir01, pts[i] - pts[p0]).sqrMagnitude;
            if (areaSq > maxAreaSq) { maxAreaSq = areaSq; p2 = i; }
        }
        if (p2 == -1)
        {
            return result;
        }

        int p3 = -1;
        float maxVol = 0f;
        Vector3 planeNormal = Vector3.Cross(dir01, pts[p2] - pts[p0]).normalized;
        for (int i = 0; i < n; i++)
        {
            if (i == p0 || i == p1 || i == p2)
            {
                continue;
            }

            float vol = Mathf.Abs(Vector3.Dot(pts[i] - pts[p0], planeNormal));
            if (vol > maxVol) { maxVol = vol; p3 = i; }
        }
        if (p3 == -1 || maxVol < 1e-6f)
        {
            return result;
        }

        Vector3 interior = (pts[p0] + pts[p1] + pts[p2] + pts[p3]) * 0.25f;
        List<HullFace> faces = new(n * 2)
        {
            new(p0, p1, p2, interior, pts),
            new(p0, p2, p3, interior, pts),
            new(p0, p3, p1, interior, pts),
            new(p1, p3, p2, interior, pts)
        };

        HashSet<long> visibleEdges = new();
        List<(int u, int v)> edgeList = new();
        List<HullFace> nextFaces = new(n * 2);

        for (int i = 0; i < n; i++)
        {
            if (i == p0 || i == p1 || i == p2 || i == p3)
            {
                continue;
            }

            Vector3 p = pts[i];
            visibleEdges.Clear();
            edgeList.Clear();
            nextFaces.Clear();

            bool anyVisible = false;
            for (int f = 0; f < faces.Count; f++)
            {
                HullFace face = faces[f];
                if (Vector3.Dot(face.Normal, p) - face.Offset > 1e-6f)
                {
                    anyVisible = true;
                    RecordEdge(face.A, face.B, visibleEdges, edgeList);
                    RecordEdge(face.B, face.C, visibleEdges, edgeList);
                    RecordEdge(face.C, face.A, visibleEdges, edgeList);
                }
                else
                {
                    nextFaces.Add(face);
                }
            }

            if (!anyVisible)
            {
                continue;
            }

            for (int e = 0; e < edgeList.Count; e++)
            {
                var (u, v) = edgeList[e];
                if (!visibleEdges.Contains(((long)v << 32) | (uint)u))
                {
                    nextFaces.Add(new(u, v, i, interior, pts));
                }
            }

            (faces, nextFaces) = (nextFaces, faces);
        }

        for (int f = 0; f < faces.Count; f++)
        {
            result.Add(faces[f].A);
            result.Add(faces[f].B);
            result.Add(faces[f].C);
        }
        return result;
    }

    private static void RecordEdge(int u, int v, HashSet<long> set, List<(int u, int v)> list)
    {
        set.Add(((long)u << 32) | (uint)v);
        list.Add((u, v));
    }

    private void ClearBuffers()
    {
        _vertsBuf.Clear();
        _colorsBuf.Clear();
        _trisBuf.Clear();
    }

    private void ApplyBuffersToMesh(Mesh mesh)
    {
        mesh.Clear();
        if (_vertsBuf.Count == 0)
        {
            return;
        }

        mesh.SetVertices(_vertsBuf);
        mesh.SetColors(_colorsBuf);
        mesh.SetTriangles(_trisBuf, 0);
        mesh.RecalculateBounds();
    }

    private static void AppendOctahedron(List<Vector3> verts, List<Color> colors, List<int> tris, Vector3 center, float r, Color col)
    {
        if (r <= 1e-5f)
        {
            return;
        }

        int b = verts.Count;
        verts.Add(center + Vector3.up * r);
        verts.Add(center + Vector3.down * r);
        verts.Add(center + Vector3.right * r);
        verts.Add(center + Vector3.left * r);
        verts.Add(center + Vector3.forward * r);
        verts.Add(center + Vector3.back * r);

        for (int i = 0; i < 6; i++)
        {
            colors.Add(col);
        }

        int[] idx =
        {
            0, 4, 2,  0, 2, 5,  0, 5, 3,  0, 3, 4,
            1, 2, 4,  1, 5, 2,  1, 3, 5,  1, 4, 3
        };
        for (int i = 0; i < idx.Length; i++)
        {
            tris.Add(b + idx[i]);
        }
    }

    private static void AppendEdgePrism(List<Vector3> verts, List<Color> colors, List<int> tris, Vector3 p0, Vector3 p1, float halfWidth, Color col)
    {
        Vector3 dir = p1 - p0;
        float lenSq = dir.sqrMagnitude;
        if (lenSq < 1e-10f || halfWidth <= 1e-5f)
        {
            return;
        }

        dir /= Mathf.Sqrt(lenSq);
        Vector3 up = Mathf.Abs(dir.y) < 0.99f ? Vector3.up : Vector3.right;
        Vector3 sideA = Vector3.Cross(dir, up).normalized * halfWidth;
        Vector3 sideB = Vector3.Cross(dir, sideA);

        int b = verts.Count;
        verts.Add(p0 + sideA);
        verts.Add(p0 - sideA * 0.5f + sideB * 0.866f);
        verts.Add(p0 - sideA * 0.5f - sideB * 0.866f);

        verts.Add(p1 + sideA);
        verts.Add(p1 - sideA * 0.5f + sideB * 0.866f);
        verts.Add(p1 - sideA * 0.5f - sideB * 0.866f);

        for (int i = 0; i < 6; i++)
        {
            colors.Add(col);
        }

        int[] idx =
        {
            0, 3, 4,  0, 4, 1,
            1, 4, 5,  1, 5, 2,
            2, 5, 3,  2, 3, 0
        };
        for (int i = 0; i < idx.Length; i++)
        {
            tris.Add(b + idx[i]);
        }
    }

    private static Mesh BuildUvSphereMesh(int lonSegments, int latSegments)
    {
        Mesh mesh = new()
        {
            name = "GuideSphereMesh"
        };
        Vector3[] verts = new Vector3[(lonSegments + 1) * (latSegments + 1)];
        Vector3[] normals = new Vector3[verts.Length];
        int[] tris = new int[lonSegments * latSegments * 6];

        int v = 0;
        for (int lat = 0; lat <= latSegments; lat++)
        {
            float vFrac = lat / (float)latSegments;
            float phi = vFrac * Mathf.PI;
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            for (int lon = 0; lon <= lonSegments; lon++)
            {
                float uFrac = lon / (float)lonSegments;
                float theta = uFrac * Mathf.PI * 2f;

                Vector3 n = new(Mathf.Cos(theta) * sinPhi, cosPhi, Mathf.Sin(theta) * sinPhi);
                verts[v] = n;
                normals[v] = n;
                v++;
            }
        }

        int t = 0;
        for (int lat = 0; lat < latSegments; lat++)
        {
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int current = lat * (lonSegments + 1) + lon;
                int next = current + lonSegments + 1;

                tris[t++] = current;
                tris[t++] = current + 1;
                tris[t++] = next;

                tris[t++] = current + 1;
                tris[t++] = next + 1;
                tris[t++] = next;
            }
        }

        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Shader GetLitShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                return urpLit;
            }

            Shader hdrpLit = Shader.Find("HDRP/Lit");
            if (hdrpLit != null)
            {
                return hdrpLit;
            }
        }
        return Shader.Find("Standard");
    }

    private static void SetMaterialTransparent(Material mat, bool transparent)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", transparent ? 1f : 0f);
            mat.SetInt("_SrcBlend", (int)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            mat.SetInt("_DstBlend", (int)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            mat.SetInt("_ZWrite", transparent ? 0 : 1);
            if (transparent)
            {
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            mat.renderQueue = transparent ? 3000 : -1;
        }
        else if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", transparent ? 3f : 0f);
            mat.SetInt("_SrcBlend", (int)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            mat.SetInt("_DstBlend", (int)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            mat.SetInt("_ZWrite", transparent ? 0 : 1);
            if (transparent)
            {
                mat.EnableKeyword("_ALPHABLEND_ON");
            }
            else
            {
                mat.DisableKeyword("_ALPHABLEND_ON");
            }

            mat.renderQueue = transparent ? 3000 : -1;
        }
    }

    private static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        x = Mathf.Clamp01(x);
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private static void DestroyAsset(Object asset)
    {
        if (asset == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(asset);
        }
        else
        {
            DestroyImmediate(asset);
        }
    }
}