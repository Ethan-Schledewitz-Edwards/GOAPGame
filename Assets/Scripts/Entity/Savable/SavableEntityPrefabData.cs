using GenericIndex;
using UnityEngine;

namespace Entities.Savable
{
	[CreateAssetMenu(fileName = "SaveData", menuName = "SaveData/SavableEntityPrefabData")]
	public class SavableEntityPrefabData : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public GameObject EntityPrefab { get; private set; }

		[SerializeField] private string m_key;
		public string ID => m_key;
	}
}
