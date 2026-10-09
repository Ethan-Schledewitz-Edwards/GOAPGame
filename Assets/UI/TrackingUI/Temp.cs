using UnityEngine;
using Interaction.Player;

public class Temp : PlayerInteractableBase<Temp>
{
	public override bool HasDisplayInfo => false;

	public override bool CanPlayerInteract => true;

	public override string InteractPrompt => "Tracking UI Test";

	public void Update()
	{
		UpdateInteractableInfo();
	}
}
