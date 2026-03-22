using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
	[field: SerializeField] public int ItemID {  get; private set; }
	[field: SerializeField] public string ItemName { get; private set; }
	[field: SerializeField] public Item ItemPrefab { get; private set; }
}
