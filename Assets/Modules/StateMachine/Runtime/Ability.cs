namespace Modules.StateMachine
{
	public abstract class Ability : StateComponent
	{
		public override bool CanChangeActivity => true;
	}
}