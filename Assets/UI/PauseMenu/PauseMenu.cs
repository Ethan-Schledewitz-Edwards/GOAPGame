using UnityEngine;

public class PauseMenu : Menu
{
	protected override void Awake()
	{
		base.Awake();

		MenuManager.PauseMenu = this;
	}

	protected override void Start()
	{
		SetMenuActive(false);
		base.Start();
	}

	public void QuitButton()
	{
		Application.Quit();
	}
}
