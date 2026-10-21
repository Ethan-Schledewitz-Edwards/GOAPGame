using Player.Core;
using UnityEngine;
using UnityEngine.Assertions;

namespace UISystem.TrackingUI
{
	public class InteractPromptTrackingUIManager : MonoBehaviour
	{
		[SerializeField] private InteractPromptTrackingUIElement m_interactPromptTrackingUIElement;

		private PlayerInteractionController m_playerInteraction;

		private void Awake()
		{
			Assert.IsNotNull(m_interactPromptTrackingUIElement, "[InteractPromptTrackingUIManager] " +
				"The reference to the interactPromptTrackingUIElement is missing.");
		}

		private void Start()
		{
			m_playerInteraction = Object.FindAnyObjectByType<PlayerInteractionController>();
			if (m_playerInteraction != null)
			{
				m_playerInteraction.ClosestInteractableUpdated += HandlePromptUpdated;
				m_playerInteraction.ClosestInteractableCleared += HandlePromptCleared;
			}

			if (m_interactPromptTrackingUIElement != null)
			{
				m_interactPromptTrackingUIElement.gameObject.SetActive(false);
			}
		}

		private void OnDestroy()
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
			if (m_interactPromptTrackingUIElement != null &&
				m_interactPromptTrackingUIElement.gameObject.activeSelf)
			{
				m_interactPromptTrackingUIElement.ElementReturned();
			}
		}
	}
}