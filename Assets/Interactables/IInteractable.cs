using System;
using UnityEngine;

namespace Interaction.Core
{
    public interface IInteractable
    {
		public string InteractPrompt { get; }

		/// <summary>
		/// The local offset relative to the interactable used to position the world-space UI prompt.
		/// </summary>
		public Vector3 LocalInteractionPromptOffset { get; }
		public bool HasTrackingDisplayInfo { get; }
		public Transform InteractableTransform { get; }
		public InteractionPoint[] InteractionPoints { get; }

		public event Action InteractableInfoUpdated;
		public event Action InteractableDestroyed;

		public void UpdateInteractableInfo();
	}
}
