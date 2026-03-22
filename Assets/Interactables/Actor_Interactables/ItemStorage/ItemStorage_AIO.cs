using UnityEngine;

public class ItemStorage_AIO : MonoBehaviour
{
	[Header("Building Configuration")]
	[SerializeField] private ItemData itemType;
	[SerializeField] private int maxCapacity = 100;

	private int currentStock = 0;
	public ItemData ItemType => itemType;

	public void AddItem(int amount)
	{
		currentStock = Mathf.Min(currentStock + amount, maxCapacity);
		Debug.Log($"Stored {amount} of {itemType.ItemName}. Total: {currentStock}");
	}
}
