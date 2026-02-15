public interface IInputHandler
{
	public virtual void SetControlsSubscription(bool isInputEnabled)
	{
		if (isInputEnabled)
			Subscribe();
		else
			UnSubscribe();
	}

	public abstract void Subscribe();

	public abstract void UnSubscribe();
}
