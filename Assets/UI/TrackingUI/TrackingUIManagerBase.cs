using Interaction.Core;
using Player.Core;
using System.Collections.Generic;
using UnityEngine;

namespace UISystem.TrackingUI
{
	public abstract class TrackingUIManagerBase<TTrackable, TUIElement> : MonoBehaviour
	where TTrackable : class, ITrackableByElement
	where TUIElement : TrackingUIElementBase
	{
		protected PlayerInteractionController m_playerInteraction;

		protected Dictionary<TTrackable, TUIElement> m_activeElements = new Dictionary<TTrackable, TUIElement>();

		protected virtual void Start()
		{
			m_playerInteraction = Object.FindAnyObjectByType<PlayerInteractionController>();
			if (m_playerInteraction != null)
			{
				m_playerInteraction.NewTrackableObjectInDisplayRange += HandleInteractableInRange;
				m_playerInteraction.TrackableObjectOutOfRange += HandleInteractableOutOfRange;
			}
		}

		protected virtual void OnDestroy()
		{
			if (m_playerInteraction != null)
			{
				m_playerInteraction.NewTrackableObjectInDisplayRange -= HandleInteractableInRange;
				m_playerInteraction.TrackableObjectOutOfRange -= HandleInteractableOutOfRange;
			}

			// Cleanup subscriptions and active elements
			foreach (var kvp in m_activeElements)
			{
				if (kvp.Key != null)
				{
					CleanupTrackableSubscriptions(kvp.Key);
				}
			}
			m_activeElements.Clear();
		}

		private void HandleInteractableInRange(ITrackableByElement trackable)
		{
			if (trackable is TTrackable typedTrackable)
			{
				if (!m_activeElements.ContainsKey(typedTrackable))
				{
					TUIElement uiElement = GetUIElement(typedTrackable);
					if (uiElement != null)
					{
						m_activeElements.Add(typedTrackable, uiElement);
						uiElement.ElementReleased();

						// Subscribe to updates specific to this trackable type
						RegisterTrackableSubscriptions(typedTrackable);

						// Setup initial position and initialize visuals
						UpdateElementPosition(typedTrackable, uiElement);
						InitializeUI(typedTrackable, uiElement);
					}
				}
			}
		}

		private void HandleInteractableOutOfRange(ITrackableByElement trackable)
		{
			if (trackable is TTrackable typedTrackable)
			{
				if (m_activeElements.TryGetValue(typedTrackable, out TUIElement uiElement))
				{
					CleanupTrackableSubscriptions(typedTrackable);

					uiElement.ResetDynamicPosition();
					uiElement.ElementReturned();

					// Allow implementation to pool or destroy the UI element
					ReleaseUIElement(uiElement);

					m_activeElements.Remove(typedTrackable);
				}
			}
		}

		protected void RefreshElement(TTrackable interactable, TUIElement uiElement)
		{
			UpdateElementPosition(interactable, uiElement);
			UpdateUI(interactable, uiElement);
		}

		private void UpdateElementPosition(TTrackable interactable, TUIElement uiElement)
		{
			if (interactable.ObjectTransform != null)
			{
				// Object position as fallback
				Vector3 targetPosition = interactable.ObjectTransform.position;

				// Prefer ElementTrackingPosition 
				if (interactable.ElementTrackingPosition != null)
					targetPosition = interactable.ElementTrackingPosition.position;

				uiElement.SetTrackingPosition(targetPosition);
			}
		}

		/// <summary>
		/// Override to subscribe to any custom update events provided by your specific trackable type.
		/// </summary>
		protected virtual void RegisterTrackableSubscriptions(TTrackable trackable) { }

		/// <summary>
		/// Override to unsubscribe from custom update events.
		/// </summary>
		protected virtual void CleanupTrackableSubscriptions(TTrackable trackable) { }

		/// <summary>
		/// Supply a UI element for the given trackable (e.g., pull from an Object Pool or Instantiate).
		/// </summary>
		protected abstract TUIElement GetUIElement(TTrackable trackable);

		/// <summary>
		/// Release the UI element (e.g., return it to the Object Pool or Destroy it).
		/// </summary>
		protected abstract void ReleaseUIElement(TUIElement uiElement);

		/// <summary>
		/// Called when the trackable is first displayed. Set up initial text, icons, etc.
		/// </summary>
		protected abstract void InitializeUI(TTrackable trackable, TUIElement uiElement);

		/// <summary>
		/// Called to refresh dynamic data.
		/// </summary>
		protected abstract void UpdateUI(TTrackable interactable, TUIElement uiElement);
	}
}