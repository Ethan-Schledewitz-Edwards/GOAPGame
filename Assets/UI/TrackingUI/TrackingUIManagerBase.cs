using Player.Core;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;

public abstract class TrackingUIManagerBase<TInteractable, TUIElement> : MonoBehaviour
	where TInteractable : class, IInteractable
	where TUIElement : TrackingUIElementBase
{
	protected PlayerInteractionController m_playerInteraction;

	protected Dictionary<TInteractable, TUIElement> m_activeElements = new Dictionary<TInteractable, TUIElement>();

	protected virtual void Start()
	{
		m_playerInteraction = Object.FindAnyObjectByType<PlayerInteractionController>();
		if (m_playerInteraction != null)
		{
			m_playerInteraction.NewInteractableInDisplayRange += HandleInteractableInRange;
			m_playerInteraction.InteractableOutOfRange += HandleInteractableOutOfRange;
		}
	}

	protected virtual void OnDestroy()
	{
		if (m_playerInteraction != null)
		{
			m_playerInteraction.NewInteractableInDisplayRange -= HandleInteractableInRange;
			m_playerInteraction.InteractableOutOfRange -= HandleInteractableOutOfRange;
		}

		// Cleanup subscriptions and active elements
		foreach (var kvp in m_activeElements)
		{
			if (kvp.Key != null)
			{
				CleanupInteractableSubscriptions(kvp.Key);
			}
		}
		m_activeElements.Clear();
	}

	private void HandleInteractableInRange(IInteractable interactable)
	{
		if (interactable is TInteractable typedInteractable && typedInteractable.HasTrackingDisplayInfo)
		{
			if (!m_activeElements.ContainsKey(typedInteractable))
			{
				TUIElement uiElement = GetUIElement(typedInteractable);
				if (uiElement != null)
				{
					m_activeElements.Add(typedInteractable, uiElement);
					uiElement.ElementReleased();

					// Subscribe to updates specific to this interactable type
					RegisterInteractableSubscriptions(typedInteractable);

					// Setup initial position and initialize visuals
					UpdateElementPosition(typedInteractable, uiElement);
					InitializeUI(typedInteractable, uiElement);
				}
			}
		}
	}

	private void HandleInteractableOutOfRange(IInteractable interactable)
	{
		if (interactable is TInteractable typedInteractable)
		{
			if (m_activeElements.TryGetValue(typedInteractable, out TUIElement uiElement))
			{
				CleanupInteractableSubscriptions(typedInteractable);

				uiElement.ResetDynamicPosition();
				uiElement.ElementReturned();

				// Allow implementation to pool or destroy the UI element
				ReleaseUIElement(uiElement);

				m_activeElements.Remove(typedInteractable);
			}
		}
	}

	protected void RefreshElement(TInteractable interactable, TUIElement uiElement)
	{
		UpdateElementPosition(interactable, uiElement);
		UpdateUI(interactable, uiElement);
	}

	private void UpdateElementPosition(TInteractable interactable, TUIElement uiElement)
	{
		if (interactable.InteractableTransform != null)
		{
			// Fallback or local offset handling if applicable
			Vector3 targetPosition = interactable.InteractableTransform.position;
			uiElement.SetTrackingPosition(targetPosition);
		}
	}

	/// <summary>
	/// Override to subscribe to any custom update events provided by your specific interactable type.
	/// </summary>
	protected virtual void RegisterInteractableSubscriptions(TInteractable interactable) { }

	/// <summary>
	/// Override to unsubscribe from custom update events.
	/// </summary>
	protected virtual void CleanupInteractableSubscriptions(TInteractable interactable) { }

	/// <summary>
	/// Supply a UI element for the given interactable (e.g., pull from an Object Pool or Instantiate).
	/// </summary>
	protected abstract TUIElement GetUIElement(TInteractable interactable);

	/// <summary>
	/// Release the UI element (e.g., return it to the Object Pool or Destroy it).
	/// </summary>
	protected abstract void ReleaseUIElement(TUIElement uiElement);

	/// <summary>
	/// Called when the interactable is first displayed. Set up initial text, icons, etc.
	/// </summary>
	protected abstract void InitializeUI(TInteractable interactable, TUIElement uiElement);

	/// <summary>
	/// Called to refresh dynamic data.
	/// </summary>
	protected abstract void UpdateUI(TInteractable interactable, TUIElement uiElement);
}