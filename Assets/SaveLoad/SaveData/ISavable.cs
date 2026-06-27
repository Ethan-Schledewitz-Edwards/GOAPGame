namespace SaveLoad.Data
{
	public interface ISaveable
	{
		string GetComponentId();
		object GenerateComponentData();
		void RestoreComponentData(object data);
	}
}