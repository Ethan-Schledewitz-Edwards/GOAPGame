using System;
using UnityEngine;

namespace Interaction.Player
{
	public interface IPlayerInteractable
	{
		public bool HasDisplayInfo { get; }
		public bool CanPlayerInteract { get; }
		public string InteractPrompt { get; }
		public Vector3 LocalPromptOffset { get; }
		public Transform Transform { get; }

		public void UpdateInteractableInfo();
	}
}
