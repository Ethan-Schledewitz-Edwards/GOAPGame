using UnityEngine;

namespace Interaction.Player
{
    public class ItemStoragePlayerInteractable : PlayerInteractableBase<ItemStoragePlayerInteractable>
    {
		public override bool HasDisplayInfo => true;

		public override bool CanPlayerInteract => false;

		public override string InteractPrompt => "";

		public void Update()
		{
			UpdateInteractableInfo();
		}
	}
}
