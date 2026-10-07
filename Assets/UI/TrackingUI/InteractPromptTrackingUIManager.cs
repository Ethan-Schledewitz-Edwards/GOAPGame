using Player.Core;
using UnityEngine;
using UnityEngine.Assertions;

public class InteractPromptTrackingUIManager : TrackingUIManagerBase<Temp, InteractPromptTrackingUIElement>
{
	[SerializeField] private InteractPromptTrackingUIElement m_interactPromptTrackingUIElement;

	private void Awake()
	{
		Assert.IsNotNull(m_interactPromptTrackingUIElement, "[InteractPromptTrackingUIManager] The reference to the " +
			"interactPromptTrackingUIElement is missing.");
	}

	protected override void Start()
	{
		if (m_playerInteraction != null)
		{
			m_playerInteraction.ClosestInteractableUpdated += HandlePromptUpdated;
			m_playerInteraction.ClosestInteractableCleared += HandlePromptCleared;
		}

		m_interactPromptTrackingUIElement.gameObject.SetActive(false);
	}

	protected override void OnDestroy()
	{
		if (m_playerInteraction != null)
		{
			m_playerInteraction.ClosestInteractableUpdated -= HandlePromptUpdated;
			m_playerInteraction.ClosestInteractableCleared -= HandlePromptCleared;
		}
	}

	private void HandlePromptUpdated(string prompt, Vector3 worldPosition)
	{
		if (!string.IsNullOrEmpty(prompt))
		{
			m_interactPromptTrackingUIElement.SetPromptText(prompt);
			m_interactPromptTrackingUIElement.SetTrackingPosition(worldPosition);

			if (!m_interactPromptTrackingUIElement.gameObject.activeSelf)
			{
				m_interactPromptTrackingUIElement.gameObject.SetActive(true);
				m_interactPromptTrackingUIElement.ElementReleased();
			}
		}
		else
		{
			HandlePromptCleared();
		}
	}

	private void HandlePromptCleared()
	{
		if (m_interactPromptTrackingUIElement.gameObject.activeSelf)
		{
			m_interactPromptTrackingUIElement.ResetDynamicPosition();
			m_interactPromptTrackingUIElement.SetPromptText(string.Empty);
			m_interactPromptTrackingUIElement.ElementReturned();
			m_interactPromptTrackingUIElement.gameObject.SetActive(false);
		}
	}

	protected override InteractPromptTrackingUIElement GetUIElement(Temp interactable)
	{
		Debug.Log("Get interact prompt");

		return m_interactPromptTrackingUIElement;
	}

	protected override void ReleaseUIElement(InteractPromptTrackingUIElement uiElement)
	{
		Debug.Log("Release");

		uiElement.gameObject.SetActive(false);
	}

	protected override void InitializeUI(Temp interactable, InteractPromptTrackingUIElement uiElement)
	{
		Debug.Log("SEt interact prompt");

		uiElement.SetPromptText(interactable.InteractPrompt);
	}

	protected override void UpdateUI(Temp interactable, InteractPromptTrackingUIElement uiElement)
	{
		Debug.Log("Update interact prompt");

		uiElement.SetPromptText(interactable.InteractPrompt);
	}
}
