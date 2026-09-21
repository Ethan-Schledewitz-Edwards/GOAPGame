namespace SaveLoad.Core
{
	public interface ISaveableComponent
	{
		string GetComponentId();
		object GenerateComponentData();
		void RestoreComponentData(object data);
	}
}