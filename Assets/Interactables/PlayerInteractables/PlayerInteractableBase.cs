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

		public Vector3 LocalPromptOffset => m_localPromptOffset;
		[SerializeField] private Vector3 m_localPromptOffset;

		public bool HasTrackingDisplayInfo => m_hasTrackingDisplayInfo;
		protected abstract bool m_hasTrackingDisplayInfo { get; }

		public Transform InteractableTransform => transform;

		public event Action InteractableDestroyed;
		public event Action OnInfoUpdated;

		protected virtual void OnDestroy()
		{
			InteractableDestroyed?.Invoke();
		}

		public virtual void UpdateInteractableInfo()
		{
			OnInfoUpdated?.Invoke();
		}
	}
}
