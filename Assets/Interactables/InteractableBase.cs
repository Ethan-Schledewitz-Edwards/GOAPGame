using System;
using UnityEngine;

namespace Interaction.Core
{
    public abstract class InteractableBase : MonoBehaviour
    {
		[Header("Settings")]
		public bool HasDisplayInfo { get; } // Does info display in the world when close to the player?

		[Header("Prompt")]
		public string InteractPrompt { get; }
		public Vector3 LocalPromptOffset { get; }

		public Transform Transform { get; }

		public event Action OnInfoUpdated;
		public event Action InteractableDestroyed;

		public virtual void OnDestroy()
		{
			InteractableDestroyed?.Invoke();
		}

		public void UpdateInteractableInfo()
		{
			OnInfoUpdated?.Invoke();
		}
	}
}
