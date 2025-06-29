using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class PlaneProjector : MonoBehaviour
{
	[SerializeField]
	private ProjectionPlane _projectionPlane = ProjectionPlane.XY;
	[SerializeField]
	private Material _material;

	private MeshRenderer _meshRenderer;
	private MeshFilter _meshFilter;

	private List<Vector3> _projectedLocalPoints = new();
	private List<Vector3> _projectedWorldPoints = new();

	public List<Vector3> ProjectedLocalPoints => _projectedLocalPoints;
	public List<Vector3> ProjectedWorldPoints => _projectedWorldPoints;


	private void Start()
	{
		InitComponents();
		GenerateProjection();
	}

	private void Update()
	{
		InitComponents();
		GenerateProjection();

		RepositionPoints();
	}

	private void InitComponents()
	{
		if (_meshFilter == null)
			_meshFilter = GetComponent<MeshFilter>();

		if (_meshRenderer == null)
			_meshRenderer = GetComponent<MeshRenderer>();

		if (_material != null)
			_meshRenderer.sharedMaterial = _material;
	}

	public void GenerateProjection()
	{
		_projectedLocalPoints.Clear();
		_projectedWorldPoints.Clear();

		foreach (Transform child in transform)
		{
			Vector3 local = transform.InverseTransformPoint(child.position);

			switch (_projectionPlane)
			{
				case ProjectionPlane.XY:
					local.z = 0;
					break;
				case ProjectionPlane.XZ:
					local.y = 0;
					break;
				case ProjectionPlane.YZ:
					local.x = 0;
					break;
			}

			_projectedLocalPoints.Add(local);
			_projectedWorldPoints.Add(transform.TransformPoint(local));
		}

		CreateMeshFromPoints(_projectedLocalPoints);
	}


	private void CreateMeshFromPoints(List<Vector3> localPoints)
	{
		if (localPoints.Count < 3)
		{
			Debug.LogWarning("Need at least 3 points to generate a mesh.");
			_meshFilter.mesh = null;
			return;
		}

		Mesh mesh = new Mesh();
		mesh.SetVertices(localPoints);

		List<int> triangles = new();
		for (int i = 1; i < localPoints.Count - 1; i++)
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

	private void RepositionPoints()
	{
		foreach (Transform child in transform)
		{
			Vector3 pos = child.localPosition;

			switch (_projectionPlane)
			{
				case ProjectionPlane.XY:
					pos.z = 0;
					break;
				case ProjectionPlane.XZ:
					pos.y = 0;
					break;
				case ProjectionPlane.YZ:
					pos.x = 0;
					break;
			}

			child.localPosition = pos;
		}
	}

	private void OnDrawGizmos()
	{
		for (int i = 0; i < _projectedWorldPoints.Count - 1; i++)
		{
			Gizmos.DrawLine(_projectedWorldPoints[i], _projectedWorldPoints[i + 1]);
		}

		Gizmos.DrawLine(_projectedWorldPoints.Last(), _projectedWorldPoints.First());
	}


	public enum ProjectionPlane
	{
		XY,
		XZ,
		YZ
	}
}