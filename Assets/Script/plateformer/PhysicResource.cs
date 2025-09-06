using System.Collections.Generic;
using Modules.CustomAttribute;
using Modules.StateMachine;
using Modules.DevLoggers;
using UnityEngine;
using System;

public class PhysicResource : Resource
{
	[Header("Set Up")]

	[SerializeField]
	private Rigidbody2D _rb;

	[Space(10)]
	[Header("Debug")]

	[SerializeField]
	[Disable]
	private Vector3 _currentVelo;

	[SerializeField]
	[Disable]
	private Vector3? _globalVelo;

	[SerializeField]
	[Disable]
	private List<ForceIdentifier> _forceToProcess;

	public void AddForce(Vector3 force) => 
		AddForce(new ForceIdentifier(force));
	public void AddForce(Vector3 force, float timeRemove, AnimationCurve curveRemove = null) => 
		AddForce(new ForceIdentifier(force, timeRemove, curveRemove));
	public void AddForce(Vector3 force, float timeAdd, float timeRemove, AnimationCurve curveAdd = null, AnimationCurve curveRemove = null) => 
		AddForce(new ForceIdentifier(force, timeAdd, timeRemove, curveAdd, curveRemove));

	private void AddForce(ForceIdentifier force)
	{
		DevLogger.Log($"Add: \n" +
			$"Force: {force.Force} \n" +
			$"Time Add: {force.TimeToAdd} \n" +
			$"Time Remove: {force.TimeToRemove}",
			nameof(PhysicResource));

		_forceToProcess.Add(force);
	}

	public void SetVelocity(Vector3 velocity)
	{
		_currentVelo = velocity;
	}

	public void SetGlobalVelocity(Vector3 velocity)
	{
		_globalVelo = velocity;
	}

	public void StopAllForce() => SetGlobalVelocity(Vector3.zero);

	public void ClearAllForce() => _forceToProcess.Clear();

	public void StopAndClearAllForce()
	{
		ClearAllForce();
		StopAllForce();
	}

	public override void FixedUpdate(float fixedDeltaTime)
	{
		base.FixedUpdate(fixedDeltaTime);

		Vector3 allForces = Vector3.zero;

		for (int i = _forceToProcess.Count - 1; i >= 0; i--)
		{
			_forceToProcess[i].UpdateForce(fixedDeltaTime);
			allForces += _forceToProcess[i].GetForce();

			if (_forceToProcess[i].IsForceComplete())
			{
				DevLogger.Log($"Force Removed \n" +
					$"Force: {_forceToProcess[i].Force}", 
					nameof(PhysicResource));

				_forceToProcess.RemoveAt(i);
			}
		}
	}
}

[Serializable]
public class ForceIdentifier
{
	public Vector3 Force = Vector3.one;

	public float TimeToAdd = 0f;
	public float TimeToRemove = 1f;

	public AnimationCurve AddForceCurve = null;
	public AnimationCurve RemoveForceCurve = null;

	private float _internalTimer = 0;

	public ForceIdentifier(Vector3 force)
	{
		Force = force;
	}

	public ForceIdentifier(Vector3 force, float timeRemove, AnimationCurve curveRemove)
	{
		Force = force;
		TimeToRemove = timeRemove;
		RemoveForceCurve = curveRemove;
	}

	public ForceIdentifier(Vector3 force, float timeAdd, float timeRemove, AnimationCurve curveAdd, AnimationCurve curveRemove)
	{
		Force = force;
		TimeToRemove = timeRemove;
		TimeToAdd = timeAdd;
		AddForceCurve = curveAdd;
		RemoveForceCurve = curveRemove;
	}

	public void UpdateForce(float deltaTime)
	{

	}

	public Vector3 GetForce()
	{
		return Force;
	}

	public bool IsForceComplete()
	{
		return _internalTimer >= TimeToRemove + TimeToAdd;
	}
}