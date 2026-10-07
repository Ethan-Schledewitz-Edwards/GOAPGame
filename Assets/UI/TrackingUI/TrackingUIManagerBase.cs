using Interaction.Player;
using Player.Core;
using System.Collections.Generic;
using UnityEngine;

public abstract class TrackingUIManagerBase<TInteractable, TUIElement> : MonoBehaviour
	where TInteractable : PlayerInteractableBase<TInteractable>
	where TUIElement : TrackingUIElementBase
{
	protected PlayerInteraction m_playerInteraction;

	protected Dictionary<TInteractable, TUIElement> m_activeElements = new Dictionary<TInteractable, TUIElement>();

	protected virtual void Start()
	{
		m_playerInteraction = Object.FindAnyObjectByType<PlayerInteraction>();
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

		// Cleanup subscriptions
		foreach (var kvp in m_activeElements)
		{
			if (kvp.Key != null)
			{
				kvp.Key.OnInfoUpdated -= HandleInfoUpdated;
				kvp.Key.InteractableDestroyed -= HandleInteractableDestroyed;
			}
		}
		m_activeElements.Clear();
	}

	private void HandleInteractableInRange(IPlayerInteractable playerInteractable)
	{
		Debug.Log("THIS SHOULD BE CALLED");

		if (playerInteractable is TInteractable interactable)
		{
			if (!m_activeElements.ContainsKey(interactable))
			{
				TUIElement uiElement = GetUIElement(interactable);
				if (uiElement != null)
				{
					m_activeElements.Add(interactable, uiElement);
					uiElement.ElementReleased();

					// Subscribe to the interactable's updates
					interactable.OnInfoUpdated += HandleInfoUpdated;
					interactable.InteractableDestroyed += HandleInteractableDestroyed;

					// Setup initial position and initialize visuals
					UpdateElementPosition(interactable, uiElement);
					InitializeUI(interactable, uiElement);
				}
			}
		}
	}

	private void HandleInteractableOutOfRange(IPlayerInteractable interactable)
	{
		if (interactable is TInteractable typedInteractable)
		{
			if (m_activeElements.TryGetValue(typedInteractable, out TUIElement uiElement))
			{
				// Unsubscribe from the interactable
				typedInteractable.OnInfoUpdated -= HandleInfoUpdated;
				typedInteractable.InteractableDestroyed -= HandleInteractableDestroyed;

				uiElement.ResetDynamicPosition();
				uiElement.ElementReturned();

				// Allow  implementation to pool or destroy the UI element
				ReleaseUIElement(uiElement);

				m_activeElements.Remove(typedInteractable);
			}
		}
	}

	private void HandleInfoUpdated(TInteractable interactable)
	{
		if (m_activeElements.TryGetValue(interactable, out TUIElement uiElement))
		{
			// Refresh position and UI data
			UpdateElementPosition(interactable, uiElement);
			UpdateUI(interactable, uiElement);
		}
	}

	private void HandleInteractableDestroyed(TInteractable interactable)
	{
		if (m_activeElements.TryGetValue(interactable, out TUIElement uiElement))
		{
			// Refresh position and UI data
			UpdateElementPosition(interactable, uiElement);
			UpdateUI(interactable, uiElement);
		}
	}

	private void UpdateElementPosition(TInteractable interactable, TUIElement uiElement)
	{
		Vector3 targetPosition = interactable.Transform.position + interactable.LocalPromptOffset;
		uiElement.SetTrackingPosition(targetPosition);
	}

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
	/// Called whenever the interactable fires UpdateInteractableInfo() to refresh dynamic data.
	/// </summary>
	protected abstract void UpdateUI(TInteractable interactable, TUIElement uiElement);
}
