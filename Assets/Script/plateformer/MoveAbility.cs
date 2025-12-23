using Modules.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAbility : Ability
{
	private PhysicResource _physic;

	public override void ComponentInit(Controller controller)
	{
		base.ComponentInit(controller);

		_physic = controller.GetStateComponent<PhysicResource>();
	}
}
