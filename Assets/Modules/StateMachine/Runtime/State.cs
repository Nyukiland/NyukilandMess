using UnityEngine.InputSystem;

namespace Modules.StateMachine
{
	public abstract class State
	{
		private Controller _controller;
		public virtual Controller Controller
		{
			get { return _controller; }
			set { _controller = value; }
		}

		protected void GetStateComponent<T>(ref T component, bool enable = true) where T : StateComponent
		{
			component ??= _controller.GetStateComponent<T>();
			if (enable)
				component?.OnEnableController();
		}

		public virtual void OnEnter() { }

		public virtual void OnExit() { }

		public virtual void Update(float deltaTime) { }

		public virtual void FixedUpdate(float fixedDeltaTime) { }

		public virtual void OnActionTriggered(InputAction.CallbackContext context) { }
	}
}