using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void RefreshSpline();

[ExecuteInEditMode]
public class PathSystem : MonoBehaviour
{
	[Header ("Settings")]
	public List<PathPoint> _controlPoints = new();

	[SerializeField, Min(0)]
	private int _resolution;

	[SerializeField]
	private bool _ended;

	[Header ("Debug")]

	[SerializeField, ReadOnly]
	List<PathStorage> _pathPoint = new();

	public event RefreshSpline RefreshSplineEvent;

	private void Update()
	{
		EditorUpdate();
	}

	void GeneratePath()
	{
		if (_controlPoints.Count < 2) return;

		_pathPoint.Clear();

		for (int i = 0; i < _controlPoints.Count; i++)
		{
			List<PathStorage> tempList = new List<PathStorage>();

			PathStorage firstPoint = new PathStorage
			{
				Pos = _controlPoints[i].MainPoint.position,
				Rot = _controlPoints[i].MainPoint.eulerAngles,
				Size = _controlPoints[i].MainPoint.localScale.x
			};

			PathStorage midPoint = new PathStorage
			{
				Pos = _controlPoints[i].MidPoint.position,
				Rot = _controlPoints[i].MidPoint.eulerAngles,
				Size = _controlPoints[i].MidPoint.localScale.x
			};

			PathStorage lastPoint = new PathStorage();

			if (_ended)
			{
				lastPoint = new PathStorage()
				{
					Pos = (i + 1 < _controlPoints.Count) ? _controlPoints[i + 1].MainPoint.position : _controlPoints[0].MainPoint.position,
					Rot = (i + 1 < _controlPoints.Count) ? _controlPoints[i + 1].MainPoint.eulerAngles : _controlPoints[0].MainPoint.eulerAngles,
					Size = (i + 1 < _controlPoints.Count) ? _controlPoints[i + 1].MainPoint.localScale.x : _controlPoints[0].MainPoint.localScale.x
				};
			}
			else
			{
				if ((i + 1 >= _controlPoints.Count)) break;

				lastPoint = new PathStorage()
				{
					Pos = _controlPoints[i + 1].MainPoint.position,
					Rot = _controlPoints[i + 1].MainPoint.eulerAngles,
					Size = _controlPoints[i + 1].MainPoint.localScale.x
				};
			}

			tempList.Add(firstPoint);

			for (int j = 0; j <= _resolution; j++)
			{
				float t = (float)j / _resolution;

				Vector3 lerpPos1 = Vector3.Lerp(firstPoint.Pos, midPoint.Pos, t);
				Vector3 lerpPos2 = Vector3.Lerp(midPoint.Pos, lastPoint.Pos, t);
				Vector3 finalPos = Vector3.Lerp(lerpPos1, lerpPos2, t);

				PathStorage splinePoint = new PathStorage
				{
					Pos = finalPos,
					Rot = Vector3.Lerp(firstPoint.Rot, lastPoint.Rot, t),
					Size = Mathf.Lerp(firstPoint.Size, lastPoint.Size, t)
				};

				tempList.Add(splinePoint);
			}

			_pathPoint.AddRange(tempList);
		}
	}

	#region Editor

	private List<TransformStorage> _previousPoints = new();

	private void OnValidate()
	{
		GeneratePath();
	}

	void EditorUpdate()
	{
#if UNITY_EDITOR
		if (Application.isPlaying) return;

		if (!IsSimilar(_controlPoints, _previousPoints) || _pathPoint.Count == 0)
		{
			GeneratePath();
			RefreshSplineEvent?.Invoke();
		}

#endif
	}

	private bool IsSimilar(List<PathPoint> current, List<TransformStorage> previous)
	{
		bool isSame = true;

		for (int i = 0; i < current.Count; i++)
		{
			if (previous.Count - 1 < i)
			{
				isSame = false;

				TransformStorage t = new();
				t.position = current[i].MainPoint.position;
				t.eulerAngle = current[i].MainPoint.eulerAngles;
				t.localScale = current[i].MainPoint.localScale;
				previous.Add(t);

				continue;
			}

			if (current[i].MainPoint.position != previous[i].position || current[i].MainPoint.eulerAngles != previous[i].eulerAngle || current[i].MainPoint.localScale != previous[i].localScale)
			{
				isSame = false;

				TransformStorage temp = new();

				temp.position = current[i].MainPoint.position;
				temp.eulerAngle = current[i].MainPoint.eulerAngles;
				temp.localScale = current[i].MainPoint.localScale;

				previous[i] = temp;
			}
		}

		return isSame;
	}

	private void OnDrawGizmos()
	{
		for (int t = 0; t < _controlPoints.Count; t++)
		{
			Gizmos.color = Color.black;
			Gizmos.DrawRay(_controlPoints[t].MainPoint.position, _controlPoints[t].MainPoint.transform.right * _controlPoints[t].MainPoint.localScale.x);
			Gizmos.DrawRay(_controlPoints[t].MainPoint.position, -_controlPoints[t].MainPoint.transform.right * _controlPoints[t].MainPoint.localScale.x);

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(_controlPoints[t].MainPoint.position, _controlPoints[t].MidPoint.position);
			if (t + 1 < _controlPoints.Count) Gizmos.DrawLine(_controlPoints[t].MidPoint.position, _controlPoints[t + 1].MainPoint.position);
			else
			{
				if (_ended) Gizmos.DrawLine(_controlPoints[t].MidPoint.position, _controlPoints[0].MainPoint.position);
			}
		}

		Gizmos.color = Color.white;
		for (int i = 0; i < _pathPoint.Count; i++)
		{
			if (i + 1 >= _pathPoint.Count)
			{
				if (_ended) Gizmos.DrawLine(_pathPoint[i].Pos, _pathPoint[0].Pos);
			}
			else Gizmos.DrawLine(_pathPoint[i].Pos, _pathPoint[i + 1].Pos);
		}
	}

	public struct TransformStorage
	{
		public Vector3 position;
		public Vector3 eulerAngle;
		public Vector3 localScale;
	}
	#endregion
}

[Serializable]
public struct PathPoint
{
	public Transform MainPoint;

	[Min(2)]
	public int _shapeCount;

	[SerializeField]
	private Transform _midPoint;
	public Transform MidPoint
	{
		get => _midPoint;
		set => _midPoint = value != null ? value : null;
	}
}

[Serializable]
public struct PathStorage
{
	public Vector3 Pos;
	public Vector3 Rot;
	public float Size;
}