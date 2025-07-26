using System.Collections.Generic;
using Modules.CustomAttribute;
using UnityEngine.InputSystem;
using UnityEngine;
using System;
using System.Linq;

namespace Modules.StateMachine
{
	public class Controller : MonoBehaviour
	{
		[Header("State")]

		[SerializeField]
		[TypeSelector(typeof(State))]
		private string _defaultState;

		[SerializeField]
		private bool _useDifferentFirstState;

		[SerializeField]
		[HideWhenBool(nameof(_useDifferentFirstState))]
		[TypeSelector(typeof(State))]
		private string _firstState;

		[Space(10)]

		[Header("Components")]

		[SerializeField]
		[SerializeReference]
		[SubClassSelector(typeof(StateComponent))]
		private List<StateComponent> _components = new();

		#region UnityMethod

		protected virtual void Awake()
		{
			_components.ForEach(comp => comp.EarlyInit());
			_components.ForEach(comp => comp.InitController(controller: this));
		}

		protected virtual void Start()
		{
			_components.ForEach(comp => comp.LateInit());
		}

		private void OnEnable()
		{
			if (!_useDifferentFirstState) SetDefaultState();
			else SetState(Type.GetType(_firstState));

			InputSystem.actions.FindActionMap("Player").Enable();
			InputSystem.actions.FindActionMap("Player").actionTriggered += OnActionTriggered;
		}

		private void OnDisable()
		{
			foreach (StateComponent comp in _components)
			{
				comp.OnDisableController();
			}

			InputSystem.actions.FindActionMap("Player").Disable();
			InputSystem.actions.FindActionMap("Player").actionTriggered -= OnActionTriggered;
		}

		public void Update()
		{
			float deltaTime = Time.deltaTime;
			_state?.Update(deltaTime);
			ComponentUpdate(deltaTime);
		}

		public void FixedUpdate()
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			_state?.FixedUpdate(fixedDeltaTime);
			ComponentFixedUpdate(fixedDeltaTime);
		}

		#endregion

		//-------------------------------------
		#region StateRelated

		private State _state;

		public Type DefaultStateType
		{
			private get => Type.GetType(_defaultState);
			set => _defaultState = value.AssemblyQualifiedName;
		}

		public string PrevState { get; set; }

		public string CurrentState { get => _state?.GetType().Name ?? "none"; }

		public State State { get => _state; private set => _state = value; }

		public bool IsInState<T>()
		{
			return _state?.GetType() == typeof(T);
		}

		public void SetDefaultState()
		{
			if (DefaultStateType != null)
				SetState(DefaultStateType);
			else
				SetState(state: null);
		}

		public void SetState<T>() where T : State
		{
			SetState(typeof(T));
		}

		public void SetState(Type type)
		{
			if (!type.IsSubclassOf(typeof(State)))
			{
				throw new ArgumentException(
					nameof(type),
					$"The type should be a subclass of {typeof(State).Name}"
				);
			}

			SetState((State)Activator.CreateInstance(type));
		}

		public T GetActionValue<T>(string actionName) where T : struct
		{
			InputAction action = InputSystem.actions.FindAction(actionName);
			return action != null ? action.ReadValue<T>() : default;
		}

		private void OnActionTriggered(InputAction.CallbackContext context)
		{
			_state?.OnActionTriggered(context);
		}

		private void SetState(State state)
		{
			_state?.OnExit();
			DisableAllAbilities();

			if (_state != null) PrevState = _state.GetType().Name;

			_state = state;
			if (_state != null)
			{
				_state.Controller = this;
				_state.OnEnter();
			}
		}

		private void DisableAllAbilities()
		{
			foreach (StateComponent component in _components)
			{
				if (component.CanChangeActivity && component.Enabled)
					component.OnDisableController();
			}
		}

		#endregion

		//-------------------------------------
		#region ComponentRelated

		public T GetStateComponent<T>() where T : StateComponent
		{
			foreach (StateComponent component in _components)
			{
				if (component is T tComponent)
					return tComponent;
			}

			UnityEngine.Debug.LogError($"[{nameof(Controller)}] No Component found for `{typeof(T).Name}`");
			return null;
		}

		private void ComponentUpdate(float deltaTime)
		{
			foreach (StateComponent component in _components)
			{
				if (!component.Enabled) continue;
				component.Update(deltaTime);
			}
		}

		private void ComponentFixedUpdate(float deltaTime)
		{
			foreach (StateComponent component in _components)
			{
				if (!component.Enabled) continue;
				component.FixedUpdate(Time.fixedDeltaTime);
			}
		}

		private void OnValidate()
		{
			for (int i = 0; i < _components.Count; i++)
			{
				if (_components[i] != null) _components[i].OnValidate();
			}
		}

		#endregion
	}
}