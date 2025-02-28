using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		List<PointFace> pointFaces = new();
		List<Vector3> baseVertices = new();
		List<Vector3> finalVertices = new();
		List<int> triangles = new();

		int globalIndex = 0;

		//generate points
		foreach (PathPoint point in _path._controlPoints)
		{
			Vector3[] points = GenerateCirclePoints(Vector3.zero, Vector3.forward, point._shapeCount);
			PointFace face = new(points.ToList(), Enumerable.Range(globalIndex, points.Length).ToList());
			pointFaces.Add(face);
			baseVertices.AddRange(points);
			globalIndex += points.Length;
		}

		finalVertices = new List<Vector3>(baseVertices);

		//draw face
		foreach (PointFace face in pointFaces)
		{
			int centerIndex = finalVertices.Count;
			face.CenterIndex = centerIndex;
			finalVertices.Add(Vector3.zero);

			for (int i = 0; i < face.Vertices.Count; i++)
			{
				int pointA = face.Indices[i];
				int pointB = face.Indices[(i + 1) % face.Vertices.Count];

				triangles.Add(pointA);
				triangles.Add(pointB);
				triangles.Add(centerIndex);
			}
		}

		//connect face i and face i + 1
		for (int i = 0; i < pointFaces.Count - 1; i++)
		{
			PointFace faceA = pointFaces[i];
			PointFace faceB = pointFaces[i + 1];

			//create triangle based on the closest vertice
			for (int j = 0; j < faceA.Vertices.Count; j++)
			{
				int indexA1 = faceA.Indices[j];
				int indexA2 = faceA.Indices[(j + 1) % faceA.Vertices.Count];
				int indexB1 = faceB.Indices[FindClosestPoint(faceB.Vertices, faceA.Vertices[j])];
				int indexB2 = faceB.Indices[FindClosestPoint(faceB.Vertices, faceA.Vertices[(j + 1) % faceA.Vertices.Count])];

				if (indexA1 < finalVertices.Count && indexA2 < finalVertices.Count &&
					indexB1 < finalVertices.Count && indexB2 < finalVertices.Count)
				{
					triangles.Add(indexA1);
					triangles.Add(indexB1);
					triangles.Add(indexA2);

					triangles.Add(indexA2);
					triangles.Add(indexB1);
					triangles.Add(indexB2);
				}
				else
				{
					Debug.LogError($"Invalid triangle indices: A1: {indexA1}, A2: {indexA2}, B1: {indexB1}, B2: {indexB2}");
				}
			}
		}

		int vertIndex = 0;

		foreach (PathPoint point in _path._controlPoints)
		{
			Quaternion rotation = Quaternion.LookRotation(point.MainPoint.forward);
			Vector3 offset = point.MainPoint.position;
			for (int j = 0; j < point._shapeCount; j++)
			{
				if (vertIndex < finalVertices.Count)
				{
					finalVertices[vertIndex] = rotation * baseVertices[vertIndex] + offset;
				}
				else
				{
					Debug.LogError($"Vertex index out of range: {vertIndex} (finalVertices size: {finalVertices.Count})");
				}
				vertIndex++;
			}
		}

		foreach (PointFace face in pointFaces)
		{
			if (face.CenterIndex < finalVertices.Count)
			{
				finalVertices[face.CenterIndex] = _path._controlPoints[pointFaces.IndexOf(face)].MainPoint.position;
			}
		}

		Mesh mesh = new();
		mesh.vertices = finalVertices.ToArray();

		for (int i = 0; i < triangles.Count; i++)
		{
			if (triangles[i] < 0 || triangles[i] >= finalVertices.Count)
			{
				Debug.LogError($"Triangle index out of bounds: {triangles[i]} (VertexCount: {finalVertices.Count})");
			}
		}

		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();

		_filter.mesh = mesh;
	}

	//get the vertice closest
	int FindClosestPoint(List<Vector3> face, Vector3 target)
	{
		int closestIndex = 0;
		float closestDistance = float.MaxValue;

		for (int i = 0; i < face.Count; i++)
		{
			float dist = Vector3.Distance(face[i], target);
			if (dist < closestDistance)
			{
				closestDistance = dist;
				closestIndex = i;
			}
		}

		return closestIndex;
	}

	//generate the point on a circle
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

	public class PointFace
	{
		public List<Vector3> Vertices;
		public List<int> Indices;
		public int CenterIndex;

		public PointFace(List<Vector3> vertices, List<int> indices)
		{
			Vertices = vertices;
			Indices = indices;
			CenterIndex = -1;
		}
	}
}