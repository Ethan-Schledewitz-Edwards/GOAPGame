using UnityEngine;

namespace Interaction.Core
{
	public interface IInteractor
	{
		public Transform Transform { get; }

		/// <summary>
		/// Initiates an interaction with a target interactable object.
		/// </summary>
		void InteractWith(IInteractable interactable, bool willReplaceJob); // Move will replace job to actor

		void BeginCarrying(Transform parent, Vector3 localPosition);

		void StopCarrying(); // Move to actor
	}
}
