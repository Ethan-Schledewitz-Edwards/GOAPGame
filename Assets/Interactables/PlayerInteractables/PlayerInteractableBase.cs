using System;
using UnityEngine;

namespace Interaction.Player
{
	public abstract class PlayerInteractableBase<T> : MonoBehaviour, IPlayerInteractable
		where T : PlayerInteractableBase<T>
	{
		public abstract bool HasDisplayInfo { get; }
		public abstract bool CanPlayerInteract { get; }
		public abstract string InteractPrompt { get; }
		[field: SerializeField] public Vector3 LocalPromptOffset { get; private set; }
		public Transform Transform => transform;

		public event Action<T> OnInfoUpdated;
		public event Action<T> InteractableDestroyed;

		public void UpdateInteractableInfo()
		{
			OnInfoUpdated?.Invoke(this as T);
		}

		public virtual void OnDestroy()
		{
			InteractableDestroyed?.Invoke(this as T);
		}
	}
}
