using UnityEngine;

public class PauseMenu : Menu
{
	protected override void Awake()
	{
		base.Awake();

		MenuManager.PauseMenu = this;
		SetMenuActive(false);
	}

	public void QuitButton()
	{
		Application.Quit();
	}
}
