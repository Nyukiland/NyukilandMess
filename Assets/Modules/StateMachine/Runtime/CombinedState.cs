using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Modules.StateMachine
{
	public abstract class CombinedState : State
	{
		private readonly List<State> _subStates = new();

		public override Controller Controller
		{
			get { return base.Controller; }
			set
			{
				foreach (State subState in _subStates)
				{
					subState.Controller = value;
				}
				base.Controller = value;
			}
		}

		protected void AddSubState(State subState)
		{
			_subStates.Add(subState);
		}

		public override void OnEnter()
		{
			base.OnEnter();
			foreach (State subState in _subStates)
			{
				subState.OnEnter();
			}
		}

		public override void OnExit()
		{
			base.OnExit();
			foreach (State subState in _subStates)
			{
				subState.OnExit();
			}
		}

		public override void Update(float deltaTime)
		{
			base.Update(deltaTime);
			foreach (State subState in _subStates)
			{
				subState.Update(deltaTime);
			}
		}

		public override void FixedUpdate(float fixedDeltaTime)
		{
			base.FixedUpdate(fixedDeltaTime);
			foreach (State subState in _subStates)
			{
				subState.FixedUpdate(fixedDeltaTime);
			}
		}

		public override void OnActionTriggered(InputAction.CallbackContext context)
		{
			base.OnActionTriggered(context);
			foreach (State subState in _subStates)
			{
				subState.OnActionTriggered(context);
			}
		}
	}
}