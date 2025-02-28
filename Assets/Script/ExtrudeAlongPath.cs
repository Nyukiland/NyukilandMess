using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Splines;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(PathSystem))]
[ExecuteInEditMode]
public class ExtrudeAlongPath : MonoBehaviour
{
	[SerializeField, ReadOnly]
	private MeshRenderer _renderer;
	[SerializeField, ReadOnly]
	private MeshFilter _filter;
	[SerializeField, ReadOnly]
	private PathSystem _path;

	private void OnEnable()
	{
		_renderer = GetComponent<MeshRenderer>();
		_filter = GetComponent<MeshFilter>();
		_path = GetComponent<PathSystem>();

		_path.RefreshSplineEvent += GenerateExtrusion;
	}

	private void OnDisable()
	{
		_path.RefreshSplineEvent -= GenerateExtrusion;
	}

	void GenerateExtrusion()
	{
		if (_renderer == null)
		{
			_renderer = GetComponent<MeshRenderer>();
			_filter = GetComponent<MeshFilter>();
			_path = GetComponent<PathSystem>();
		}
		
		GenerateWithPoint();
	}

	void GenerateWithPoint()
	{
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();

		foreach (PathPoint point in _path._controlPoints)
		{
			int index = Mathf.Clamp(vertices.Count, 0, 1000000);
			Vector3[] points = GenerateCirclePoints(point.MainPoint.position, point.MainPoint.forward, point._shapeCount);
			vertices.AddRange(points);
			vertices.Add(point.MainPoint.position);

			for (int i = index; i < vertices.Count -1; i++)
			{
				int pointA = i;
				int pointB = i + 1;
				if (pointB >= vertices.Count-1) pointB = index;

				triangles.Add(pointA);
				triangles.Add(pointB);
				triangles.Add(vertices.Count-1);
			}
		}

		Mesh mesh = new();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();

		mesh.RecalculateNormals();

		_filter.mesh = mesh;
	}

	Vector3[] GenerateCirclePoints(Vector3 position, Vector3 forward, int numPoints, float radius = 1f)
	{
		Vector3[] points = new Vector3[numPoints];

		Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
		Vector3 up = Vector3.Cross(right, forward).normalized;

		for (int i = 0; i < numPoints; i++)
		{
			float angle = (360f / numPoints) * i;
			float radian = angle * Mathf.Deg2Rad;

			Vector3 point = position + (right * Mathf.Cos(radian) + up * Mathf.Sin(radian)) * radius;
			points[i] = point;
		}

		return points;
	}
}
