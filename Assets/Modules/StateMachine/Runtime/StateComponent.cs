using UnityEngine;

namespace Modules.StateMachine
{
	[System.Serializable]
	public abstract class StateComponent
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
				OnEnable();
			}

			Init(controller);
		}


		public virtual void Init(Controller controller) { }

		public virtual void LateInit() { }

		public void SetActive(bool value)
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

			OnEnable();
		}

		public void OnDisableController()
		{
			if (Enabled == false)
				return;

			if (CanChangeActivity) Enabled = false;

			OnDisable();
		}

		protected virtual void OnEnable() { }

		protected virtual void OnDisable() { }

		public virtual void Update(float deltaTime) { }

		public virtual void FixedUpdate(float fixedDeltaTime) { }

		public virtual void OnValidate() { }
	}
}