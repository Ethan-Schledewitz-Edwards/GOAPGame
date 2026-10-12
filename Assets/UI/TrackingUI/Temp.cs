using Interaction.Player;

public class Temp : PlayerInteractableBase
{
	protected override string m_interactPrompt => "Tracking UI Test";

	protected override bool m_hasTrackingDisplayInfo => false;

	public void Update()
	{
		UpdateInteractableInfo();
	}
}
