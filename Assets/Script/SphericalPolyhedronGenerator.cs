using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent( typeof(MeshRenderer))]
public class SphericalPolyhedronGenerator : MonoBehaviour
{
    [Header("Polyhedron Settings")]
    [Min(4)]
    [SerializeField] 
    private int _faceCount = 6;

    [Min(0.01f)]
    [SerializeField] 
    private float _radius = 1f;

    [Range(0, 50)]
    [SerializeField] 
    private int _relaxationIterations = 12;

    [SerializeField] 
    private bool _flatShaded = true;

    private Mesh _mesh;
    private MeshFilter _meshFilter;

#if UNITY_EDITOR
    private bool _regenerationQueued;
#endif

    public int FaceCount
    {
        get => _faceCount;
        set { _faceCount = Mathf.Max(4, value); Regenerate(); }
    }

    public float Radius
    {
        get => _radius;
        set { _radius = Mathf.Max(0.01f, value); Regenerate(); }
    }

    public int RelaxationIterations
    {
        get => _relaxationIterations;
        set { _relaxationIterations = Mathf.Clamp(value, 0, 30); Regenerate(); }
    }

    private void Awake()
    {
        Regenerate();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _faceCount = Mathf.Max(4, _faceCount);
        _radius = Mathf.Max(0.01f, _radius);

        if (_regenerationQueued)
        {
            return;
        }

        _regenerationQueued = true;

        EditorApplication.delayCall += () =>
        {
            _regenerationQueued = false;
            if (this != null)
            {
                Regenerate();
            }
        };
    }
#endif

    private void OnDestroy()
    {
        if (_mesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_mesh);
        }
        else
        {
            DestroyImmediate(_mesh);
        }
    }

    public void Regenerate()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = $"SphericalPolyhedron_{_faceCount}" };
            _mesh.MarkDynamic();
        }
        else
        {
            _mesh.Clear();
            _mesh.name = $"SphericalPolyhedron_{_faceCount}";
        }

        BuildPolyhedronMesh(_mesh, Mathf.Max(4, _faceCount), Mathf.Max(0.01f, _radius), _relaxationIterations, _flatShaded);

        _meshFilter.sharedMesh = _mesh;
    }

    private static void BuildPolyhedronMesh(Mesh mesh, int count, float sphereRadius, int iterations, bool flat)
    {
        Vector3[] seeds = GenerateFibonacciPoints(count);

        List<int> hullTris = null;
        for (int iter = 0; iter < iterations; iter++)
        {
            hullTris = ComputeConvexHull(seeds);
            if (hullTris.Count < 12)
            {
                return;
            }

            seeds = RelaxSeeds(seeds, hullTris);
        }

        if (hullTris == null || iterations > 0)
        {
            hullTris = ComputeConvexHull(seeds);
        }

        if (hullTris.Count < 12)
        {
            return;
        }

        int hullTriCount = hullTris.Count / 3;
        Vector3[] dualVerts = new Vector3[hullTriCount];

        for (int t = 0; t < hullTriCount; t++)
        {
            Vector3 a = seeds[hullTris[t * 3]];
            Vector3 b = seeds[hullTris[t * 3 + 1]];
            Vector3 c = seeds[hullTris[t * 3 + 2]];

            Vector3 circumcenter = Vector3.Cross(b - a, c - a);
            if (circumcenter.sqrMagnitude < 1e-12f)
            {
                circumcenter = (a + b + c);
            }

            circumcenter.Normalize();
            if (Vector3.Dot(circumcenter, a + b + c) < 0f)
            {
                circumcenter = -circumcenter;
            }

            dualVerts[t] = circumcenter * sphereRadius;
        }

        List<int>[] seedToDualVerts = new List<int>[count];
        for (int i = 0; i < count; i++)
        {
            seedToDualVerts[i] = new List<int>(6);
        }

        for (int t = 0; t < hullTriCount; t++)
        {
            seedToDualVerts[hullTris[t * 3]].Add(t);
            seedToDualVerts[hullTris[t * 3 + 1]].Add(t);
            seedToDualVerts[hullTris[t * 3 + 2]].Add(t);
        }

        if (!flat)
        {
            List<int> smoothTris = new List<int>(hullTriCount * 3);
            Vector3[] smoothNormals = new Vector3[hullTriCount];
            for (int i = 0; i < hullTriCount; i++)
            {
                smoothNormals[i] = dualVerts[i].normalized;
            }

            for (int s = 0; s < count; s++)
            {
                List<int> ring = seedToDualVerts[s];
                if (ring.Count < 3)
                {
                    continue;
                }

                SortRingClockwise(ring, dualVerts, seeds[s]);

                int root = ring[0];
                for (int i = 1; i < ring.Count - 1; i++)
                {
                    AddOrientedTriangle(smoothTris, root, ring[i], ring[i + 1], dualVerts, seeds[s]);
                }
            }

            mesh.vertices = dualVerts;
            mesh.normals = smoothNormals;
            mesh.triangles = smoothTris.ToArray();
            mesh.RecalculateBounds();
            return;
        }

        List<Vector3> vertices = new List<Vector3>(hullTriCount * 3);
        List<Vector3> normals = new List<Vector3>(hullTriCount * 3);
        List<Vector2> uvs = new List<Vector2>(hullTriCount * 3);
        List<int> triangles = new List<int>(hullTriCount * 3);

        for (int s = 0; s < count; s++)
        {
            List<int> ring = seedToDualVerts[s];
            if (ring.Count < 3)
            {
                continue;
            }

            Vector3 faceNormal = seeds[s];
            SortRingClockwise(ring, dualVerts, faceNormal);

            Vector3 tangent = Vector3.Cross(faceNormal, Mathf.Abs(faceNormal.y) < 0.99f ? Vector3.up : Vector3.right).normalized;
            Vector3 bitangent = Vector3.Cross(faceNormal, tangent);

            int baseIdx = vertices.Count;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 v = dualVerts[ring[i]];
                vertices.Add(v);
                normals.Add(faceNormal);

                Vector3 rel = v / sphereRadius;
                uvs.Add(new Vector2(Vector3.Dot(rel, tangent) * 0.5f + 0.5f, Vector3.Dot(rel, bitangent) * 0.5f + 0.5f));
            }

            for (int i = 1; i < ring.Count - 1; i++)
            {
                AddOrientedTriangle(triangles, baseIdx, baseIdx + i, baseIdx + i + 1, vertices, faceNormal);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
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

    private static Vector3[] RelaxSeeds(Vector3[] seeds, List<int> hullTris)
    {
        int n = seeds.Length;
        int triCount = hullTris.Count / 3;

        Vector3[] dualVerts = new Vector3[triCount];
        List<int>[] seedToDual = new List<int>[n];
        for (int i = 0; i < n; i++)
        {
            seedToDual[i] = new List<int>(6);
        }

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

            dualVerts[t] = cc;
            seedToDual[ia].Add(t);
            seedToDual[ib].Add(t);
            seedToDual[ic].Add(t);
        }

        Vector3[] nextSeeds = new Vector3[n];
        for (int s = 0; s < n; s++)
        {
            List<int> ring = seedToDual[s];
            if (ring.Count < 3)
            {
                nextSeeds[s] = seeds[s];
                continue;
            }

            SortRingClockwise(ring, dualVerts, seeds[s]);

            Vector3 weightedSum = Vector3.zero;
            float totalArea = 0f;
            Vector3 center = seeds[s];

            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 v0 = dualVerts[ring[i]];
                Vector3 v1 = dualVerts[ring[(i + 1) % ring.Count]];

                float area = Vector3.Cross(v0 - center, v1 - center).magnitude * 0.5f;
                Vector3 triCentroid = (center + v0 + v1) / 3f;

                weightedSum += triCentroid * area;
                totalArea += area;
            }

            nextSeeds[s] = totalArea > 1e-8f ? (weightedSum / totalArea).normalized : seeds[s];
        }

        return nextSeeds;
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

    private static void AddOrientedTriangle(List<int> tris, int i0, int i1, int i2, IList<Vector3> verts, Vector3 expectedNormal)
    {
        Vector3 cross = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
        if (Vector3.Dot(cross, expectedNormal) >= 0f)
        {
            tris.Add(i0);
            tris.Add(i1);
            tris.Add(i2);
        }
        else
        {
            tris.Add(i0);
            tris.Add(i2);
            tris.Add(i1);
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
        List<int> result = new List<int>();
        if (n < 4)
        {
            return result;
        }

        int p0 = 0;
        int p1 = 1;
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

        List<HullFace> faces = new List<HullFace>(n * 2)
        {
            new HullFace(p0, p1, p2, interior, pts),
            new HullFace(p0, p2, p3, interior, pts),
            new HullFace(p0, p3, p1, interior, pts),
            new HullFace(p1, p3, p2, interior, pts)
        };

        HashSet<long> visibleEdges = new HashSet<long>();
        List<(int u, int v)> edgeList = new List<(int u, int v)>();
        List<HullFace> nextFaces = new List<HullFace>(n * 2);

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
                if (!visibleEdges.Contains(PackEdge(v, u)))
                {
                    nextFaces.Add(new HullFace(u, v, i, interior, pts));
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
        set.Add(PackEdge(u, v));
        list.Add((u, v));
    }

    private static long PackEdge(int u, int v) => ((long)u << 32) | (uint)v;
}