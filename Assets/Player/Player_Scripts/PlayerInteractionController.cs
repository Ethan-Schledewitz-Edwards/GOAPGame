using UnityEngine;
using Interaction.Core;
using System.Collections.Generic;
using System;
using Interaction.Player;

namespace Player.Core
{
	public class PlayerInteractionController : MonoBehaviour, IInputHandler
	{
		private const float c_displayRange = 12f;
		private const float c_interactionRange = 4f;
		private const int c_maxColliders = 16;

		[SerializeField] private LayerMask m_interactionLayer;

		// Trackable Object Events
		public event Action<ITrackableObject> NewTrackableObjectInDisplayRange;
		public event Action<ITrackableObject> TrackableObjectOutOfRange;

		// Interaction Events
		public event Action<string, Vector3> ClosestInteractableUpdated;
		public event Action ClosestInteractableCleared;

		// System
		private PlayerInteractableBase m_closestInteractable;
		private Collider[] m_colliderBuffer = new Collider[c_maxColliders];

		private List<ITrackableObject> m_trackablesBuffer = new List<ITrackableObject>();
		private HashSet<ITrackableObject> m_previousTrackables = new HashSet<ITrackableObject>();
		private HashSet<ITrackableObject> m_currentTrackables = new HashSet<ITrackableObject>();

		// TO do: Interaction input using these methods
		public void Subscribe() { }
		public void UnSubscribe() { }

		private void Update()
		{
			CheckForInteractables();
		}

		private void CheckForInteractables()
		{
			int hitCount = Physics.OverlapSphereNonAlloc(transform.position, c_displayRange, m_colliderBuffer, m_interactionLayer);

			PlayerInteractableBase closestInteractable = null;
			float minSqrDistance = c_interactionRange * c_interactionRange;

			for (int i = 0; i < hitCount; i++)
			{
				Collider col = m_colliderBuffer[i];

				// Any object implementing ITrackableObject with tracking enabled gets tracked UI.
				col.GetComponents(m_trackablesBuffer);
				for (int j = 0; j < m_trackablesBuffer.Count; j++)
				{
					ITrackableObject trackableObject = m_trackablesBuffer[j];
					if (trackableObject != null && trackableObject.IsTrackingEnabled)
					{
						m_currentTrackables.Add(trackableObject);

						if (!m_previousTrackables.Contains(trackableObject))
						{
							NewTrackableObjectInDisplayRange?.Invoke(trackableObject);
						}
					}
				}
				m_trackablesBuffer.Clear();

				// Find the closest player interactable
				if (col.TryGetComponent(out PlayerInteractableBase playerInteractable))
				{
					float sqrDistance = (col.transform.position - transform.position).sqrMagnitude;
					if (sqrDistance <= minSqrDistance)
					{
						closestInteractable = playerInteractable;
						minSqrDistance = sqrDistance;
					}
				}
			}

			// Find trackable objects that left the display range
			foreach (ITrackableObject prevTrackable in m_previousTrackables)
			{
				if (prevTrackable != null &&
					!m_currentTrackables.Contains(prevTrackable))
				{
					TrackableObjectOutOfRange?.Invoke(prevTrackable);
				}
			}

			// Handle closest interactable prompt updates
			if (closestInteractable != m_closestInteractable)
			{
				if (m_closestInteractable != null)
					ClearInteractable();

				if (closestInteractable != null)
					SetInteractable(closestInteractable);
			}

			// Double buffering swap for trackables
			HashSet<ITrackableObject> temp = m_previousTrackables;
			m_previousTrackables = m_currentTrackables;
			m_currentTrackables = temp;
			m_currentTrackables.Clear();
		}

		private void SetInteractable(PlayerInteractableBase playerInteractable)
		{
			if (playerInteractable != null)
			{
				m_closestInteractable = playerInteractable;

				Vector3 promptPosition = playerInteractable.transform.position +
					playerInteractable.LocalInteractionPromptOffset;

				ClosestInteractableUpdated?.Invoke(playerInteractable.InteractPrompt, promptPosition);
			}
		}

		private void ClearInteractable()
		{
			m_closestInteractable = null;
			ClosestInteractableCleared?.Invoke();
		}
	}
}