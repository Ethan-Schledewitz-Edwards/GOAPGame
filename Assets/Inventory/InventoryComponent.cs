using UnityEngine;

[System.Serializable]
public class InventoryComponent : MonoBehaviour
{
	public Inventory Inventory { get; private set; }
	[SerializeField] private int m_inventorySize = 1;

	protected virtual void Awake()
	{
		Inventory = new Inventory(m_inventorySize);
	}
}