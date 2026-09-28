using GenericIndex;
using UnityEngine;

namespace Entities.Savable
{
	[CreateAssetMenu(fileName = "SaveData", menuName = "SaveData/SavableEntityPrefabData")]
	public class SavableEntityPrefabData : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public GameObject EntityPrefab { get; private set; }

		[SerializeField] private string m_id;
		public string ID => m_id;

#if UNITY_EDITOR
		public void SetID(string id) => m_id = id;
#endif
	}
}
