using Interaction.Core;
using System;
using UnityEngine;

namespace Interaction.Player
{
	public abstract class PlayerInteractableBase : MonoBehaviour, IInteractable
	{
		// IInteractable properties
		public string InteractPrompt => m_interactPrompt;
		protected abstract string m_interactPrompt { get; }

		public Vector3 LocalInteractionPromptOffset => m_localPromptOffset;
		[SerializeField] private Vector3 m_localPromptOffset;

		public bool HasTrackingDisplayInfo => m_hasTrackingDisplayInfo;
		protected abstract bool m_hasTrackingDisplayInfo { get; }

		public Transform InteractableTransform => transform;

		public InteractionPoint[] InteractionPoints => m_interactionPoints;
		private InteractionPoint[] m_interactionPoints;

		public event Action InteractableDestroyed;
		public event Action InteractableInfoUpdated;

		protected virtual void OnDestroy()
		{
			InteractableDestroyed?.Invoke();
		}

		public virtual void UpdateInteractableInfo()
		{
			InteractableInfoUpdated?.Invoke();
		}
	}
}
