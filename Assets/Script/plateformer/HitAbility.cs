using Modules.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitAbility : Ability
{
	private PhysicResource _physic;

	public override void Init(Controller controller)
	{
		base.Init(controller);

		_physic = controller.GetStateComponent<PhysicResource>();
	}
}
