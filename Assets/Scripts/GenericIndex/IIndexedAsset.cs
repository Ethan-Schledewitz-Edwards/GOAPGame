namespace GenericIndex
{
	public interface IIndexedAsset
	{
		string ID { get; }

#if UNITY_EDITOR
		public void SetID(string id);
#endif
	}
}