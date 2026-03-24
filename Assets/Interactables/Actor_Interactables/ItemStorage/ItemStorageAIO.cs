using UnityEngine;

[RequireComponent (typeof(InventoryComponent))]
public class ItemStorageAIO : MonoBehaviour
{
	public InventoryComponent InventoryComponent { get; private set; }

	[Header("Building Configuration")]
	[field: SerializeField] public Transform DepositPosition {  get; private set; }

	[SerializeField] private ItemData m_itemType;
	public ItemData ItemType => m_itemType;

	private void Awake()
	{
		InventoryComponent = GetComponent<InventoryComponent>();
	}
}