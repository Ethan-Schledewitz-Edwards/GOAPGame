using Player.Core;
using UnityEngine;
using UnityEngine.Assertions;

public class ItemStorageTrackingUIManager : TrackingUIManagerBase<Temp, ItemStorageTrackingUIElement>
{
	[SerializeField] private ItemStorageTrackingUIElement m_prefab;

	private void Awake()
	{
		Assert.IsNotNull(m_prefab, "[ItemStorageTrackingUIManager] The reference to the " +
			$"{typeof(ItemStorageTrackingUIElement)} prefab is missing.");
	}

	protected override ItemStorageTrackingUIElement GetUIElement(Temp interactable)
	{
		Debug.Log("Get interact prompt");

		return null;
	}

	protected override void ReleaseUIElement(ItemStorageTrackingUIElement uiElement)
	{
		Debug.Log("Release");

		uiElement.gameObject.SetActive(false);
	}

	protected override void InitializeUI(Temp interactable, ItemStorageTrackingUIElement uiElement)
	{
		Debug.Log("SEt interact prompt");

		uiElement.SetPromptText(interactable.InteractPrompt);
	}

	protected override void UpdateUI(Temp interactable, ItemStorageTrackingUIElement uiElement)
	{
		Debug.Log("Update interact prompt");

		uiElement.SetPromptText(interactable.InteractPrompt);
	}
}
