using UnityEngine;

namespace Modules.StateMachine
{
	[System.Serializable]
	public abstract class StateComponent : MonoBehaviour
	{
		public bool Enabled { get; set; } = false;

		public virtual bool CanChangeActivity => false;

		public Controller Controller { get; private set; }

		public virtual void EarlyInit() { }

		public void InitController(Controller controller)
		{
			Controller = controller;

			if (!CanChangeActivity)
			{
				Enabled = true;
				ComponentOnEnable();
			}

			ComponentInit(controller);
		}


		public virtual void ComponentInit(Controller controller) { }

		public virtual void ComponentLateInit() { }

		public void ComponentSetActive(bool value)
		{
			if (value)
				OnEnableController();
			else
				OnEnableController();
		}

		public void OnEnableController()
		{
			if (Enabled == true)
				return;

			if (CanChangeActivity) Enabled = true;

			ComponentOnEnable();
		}

		public void OnDisableController()
		{
			if (Enabled == false)
				return;

			if (CanChangeActivity) Enabled = false;

			ComponentOnDisable();
		}

		protected virtual void ComponentOnEnable() { }

		protected virtual void ComponentOnDisable() { }

		public virtual void ComponentUpdate(float deltaTime) { }

		public virtual void ComponentFixedUpdate(float fixedDeltaTime) { }
	}
}