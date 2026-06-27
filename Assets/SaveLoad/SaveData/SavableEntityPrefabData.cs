using GenericIndex;
using UnityEngine;

namespace SaveLoad.Data
{
	[CreateAssetMenu(fileName = "SaveData", menuName = "SaveData/SavableEntityPrefabData")]
	public class SavableEntityPrefabData : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public int PrefabID { get; private set; }
		[field: SerializeField] public GameObject EntityPrefab { get; private set; }

		public void SetID(int newID)
		{
			PrefabID = newID;
		}
	}
}
