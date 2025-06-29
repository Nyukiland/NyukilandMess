using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProjectionCombiner : MonoBehaviour
{
	[SerializeField] private PlaneProjector XY;
	[SerializeField] private PlaneProjector XZ;
	[SerializeField] private Material material;

	private MeshFilter _meshFilter;
	private MeshRenderer _meshRenderer;

	public float tolerance = 0.001f;

	private void Start()
	{
		Init();
		CombineProjections();
	}

	private void Update()
	{
		Init();
		CombineProjections();
	}

	private void Init()
	{
		if (_meshFilter == null)
			_meshFilter = GetComponent<MeshFilter>();

		if (_meshRenderer == null)
			_meshRenderer = GetComponent<MeshRenderer>();

		if (material != null)
			_meshRenderer.sharedMaterial = material;
	}

	private void CombineProjections()
	{
		if (XY == null || XZ == null) return;

		var xyPoints = XY.ProjectedLocalPoints.Select(p => new Vector2(p.x, p.y)).ToList();
		var xzPoints = XZ.ProjectedLocalPoints.Select(p => new Vector2(p.x, p.z)).ToList();

		List<Vector3> resultPoints = new();

		foreach (var xy in xyPoints)
		{
			foreach (var xz in xzPoints)
			{
				if (Mathf.Abs(xy.x - xz.x) < tolerance)
				{
					Vector3 combined = new Vector3(xy.x, xy.y, xz.y);
					resultPoints.Add(combined);
				}
			}
		}

		if (resultPoints.Count < 3)
		{
			_meshFilter.sharedMesh = null;
			return;
		}

		Mesh mesh = new Mesh();
		mesh.SetVertices(resultPoints);

		List<int> triangles = new();
		for (int i = 1; i < resultPoints.Count - 1; i++)
		{
			triangles.Add(0);
			triangles.Add(i);
			triangles.Add(i + 1);
		}

		mesh.SetTriangles(triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		_meshFilter.sharedMesh = mesh;
	}
}