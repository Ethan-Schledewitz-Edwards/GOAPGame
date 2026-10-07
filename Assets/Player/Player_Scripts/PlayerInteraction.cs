using UnityEngine;
using Interaction.Player;
using System.Collections.Generic;
using System;

namespace Player.Core
{
	public class PlayerInteraction : MonoBehaviour, IInputHandler
	{
		private const float c_displayRange = 12f;
		private const float c_interactionRange = 3f;
		private const int c_maxColliders = 16;

		[SerializeField] private LayerMask m_interactionLayer;

		// Display Range Events
		public event Action<IPlayerInteractable> NewInteractableInDisplayRange;
		public event Action<IPlayerInteractable> InteractableOutOfRange;

		// Interaction Events
		public event Action<string, Vector3> ClosestInteractableUpdated;
		public event Action ClosestInteractableCleared;

		private IPlayerInteractable m_closestInteractable;
		private Collider[] m_colliderBuffer = new Collider[c_maxColliders];

		private HashSet<IPlayerInteractable> m_previousInteractables = new HashSet<IPlayerInteractable>();
		private HashSet<IPlayerInteractable> m_currentInteractables = new HashSet<IPlayerInteractable>();

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

			IPlayerInteractable closestInteractable = null;
			float minSqrDistance = c_interactionRange * c_interactionRange;

			for (int i = 0; i < hitCount; i++)
			{
				Collider col = m_colliderBuffer[i];

				if (col.TryGetComponent(out IPlayerInteractable interactable))
				{
					m_currentInteractables.Add(interactable);

					// New interactable within display range
					if (interactable.HasDisplayInfo && 
						!m_previousInteractables.Contains(interactable))
					{
						NewInteractableInDisplayRange?.Invoke(interactable);
					}

					if (!interactable.CanPlayerInteract)
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
			foreach (IPlayerInteractable prevInteractable in m_previousInteractables)
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
					SetInteractable(closestInteractable);
			}

			// Double buffering swap
			HashSet<IPlayerInteractable> temp = m_previousInteractables;
			m_previousInteractables = m_currentInteractables;
			m_currentInteractables = temp;
			m_currentInteractables.Clear();
		}

		private void SetInteractable(IPlayerInteractable interactable)
		{
			if (interactable != null)
			{
				m_closestInteractable = interactable;
				Vector3 promptPosition = interactable.Transform.position + interactable.LocalPromptOffset;
				ClosestInteractableUpdated?.Invoke(interactable.InteractPrompt, promptPosition);
			}
		}

		private void ClearInteractable()
		{
			m_closestInteractable = null;
			ClosestInteractableCleared?.Invoke();
		}
	}
}