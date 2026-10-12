using System;
using UnityEngine;

namespace Interaction.Core
{
    public interface IInteractable
    {
		public string InteractPrompt { get; }
		public Vector3 LocalPromptOffset { get; }
		public bool HasTrackingDisplayInfo { get; }

		public event Action OnInfoUpdated;
		public event Action InteractableDestroyed;

		public void UpdateInteractableInfo();
	}
}
