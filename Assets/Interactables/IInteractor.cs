using UnityEngine;

namespace Interaction.Core
{
	public interface IInteractor
	{
		public Transform Transform { get; }

		/// <summary>
		/// Initiates an interaction with a target interactable object.
		/// </summary>
		void UseInteractable(IInteractable interactable);

		void InteractionCanceled();
	}
}
