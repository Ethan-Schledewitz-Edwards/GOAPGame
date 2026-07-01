namespace GenericIndex
{
	public interface IIndexedAsset
	{

#if UNITY_EDITOR
		void SetID(int newID);
#endif
	}
}