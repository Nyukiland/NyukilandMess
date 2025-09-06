using Modules.CustomAttribute;
using Modules.DevLoggers;
using Modules.StateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicResource : Resource
{
	[Header("Set Up")]

	[SerializeField]
	private Rigidbody2D _rb;

	[Space(10)]
	[Header("Debug")]

	[SerializeField]
	[Disable]
	private List<ForceIdentifier> _forceToProcess;
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

	public ForceIdentifier(Vector3 force, float timeRemove)
	{
		Force = force;
		TimeToRemove = timeRemove;
	}

	public ForceIdentifier(Vector3 force, float timeRemove, AnimationCurve curveRemove)
	{
		Force = force;
		TimeToRemove = timeRemove;
		RemoveForceCurve = curveRemove;
	}

	public ForceIdentifier(Vector3 force, float timeAdd, float timeRemove)
	{
		Force = force;
		TimeToRemove = timeRemove;
		TimeToAdd = timeAdd;
	}

	public ForceIdentifier(Vector3 force, float timeAdd, float timeRemove, AnimationCurve curveAdd, AnimationCurve curveRemove)
	{
		Force = force;
		TimeToRemove = timeRemove;
		TimeToAdd = timeAdd;
		AddForceCurve = curveAdd;
		RemoveForceCurve = curveRemove;
	}
}