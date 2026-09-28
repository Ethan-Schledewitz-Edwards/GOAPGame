namespace SaveLoad.Core
{
	public interface ISaveableComponent
	{
		string GetComponentId();
		string GenerateComponentData();
		void RestoreComponentData(object data);
	}
}