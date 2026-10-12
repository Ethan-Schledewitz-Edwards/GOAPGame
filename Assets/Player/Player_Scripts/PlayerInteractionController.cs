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
		private const float c_interactionRange = 3f;
		private const int c_maxColliders = 16;

		[SerializeField] private LayerMask m_interactionLayer;

		// Display Range Events
		public event Action<IInteractable> NewInteractableInDisplayRange;
		public event Action<IInteractable> InteractableOutOfRange;

		// Interaction Events
		public event Action<string, Vector3> ClosestInteractableUpdated;
		public event Action ClosestInteractableCleared;

		private IInteractable m_closestInteractable;
		private Collider[] m_colliderBuffer = new Collider[c_maxColliders];

		private HashSet<IInteractable> m_previousInteractables = new HashSet<IInteractable>();
		private HashSet<IInteractable> m_currentInteractables = new HashSet<IInteractable>();

		public void Subscribe()
		{
			
		}

		public void UnSubscribe()
		{
			
		}

		private void Update()
		{
			CheckForInteractables();
		}

		private void CheckForInteractables()
		{
			int hitCount = Physics.OverlapSphereNonAlloc(transform.position, c_displayRange, m_colliderBuffer, m_interactionLayer);

			IInteractable closestInteractable = null;
			float minSqrDistance = c_interactionRange * c_interactionRange;

			for (int i = 0; i < hitCount; i++)
			{
				Collider col = m_colliderBuffer[i];

				if (col.TryGetComponent(out IInteractable interactable))
				{
					m_currentInteractables.Add(interactable);

					// New interactable within display range
					if (interactable.HasTrackingDisplayInfo &&
						!m_previousInteractables.Contains(interactable))
					{
						NewInteractableInDisplayRange?.Invoke(interactable);
					}

					// Only PlayerInteractables allow direct player interactions & prompts
					if (interactable is not PlayerInteractableBase)
						continue;

					// Calculate distance for interaction prompt
					float sqrDistance = (col.transform.position - transform.position).sqrMagnitude;
					if (sqrDistance <= minSqrDistance)
					{
						closestInteractable = interactable;
						minSqrDistance = sqrDistance;
					}
				}
			}

			// Find interactables that left display range
			foreach (IInteractable prevInteractable in m_previousInteractables)
			{
				if (prevInteractable != null &&
					!m_currentInteractables.Contains(prevInteractable))
				{
					InteractableOutOfRange?.Invoke(prevInteractable);
				}
			}

			// Handle closest interactable updates
			if (closestInteractable != m_closestInteractable)
			{
				if (m_closestInteractable != null)
					ClearInteractable();

				if (closestInteractable != null)
					SetInteractable(closestInteractable as PlayerInteractableBase);
			}

			// Double buffering swap
			HashSet<IInteractable> temp = m_previousInteractables;
			m_previousInteractables = m_currentInteractables;
			m_currentInteractables = temp;
			m_currentInteractables.Clear();
		}

		private void SetInteractable(PlayerInteractableBase playerInteractable)
		{
			if (playerInteractable != null)
			{
				m_closestInteractable = playerInteractable;

				Vector3 promptPosition = playerInteractable.transform.position +
					playerInteractable.LocalPromptOffset;

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