using Modules.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCombinedState : CombinedState
{
    public BaseCombinedState()
	{
		AddSubState(new HitSubState());
	}

}
