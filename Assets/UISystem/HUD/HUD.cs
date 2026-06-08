using UnityEngine;

public class HUD : Menu
{
	public override bool IsUnclosable => true;
	public override bool IsHUD => true;


	#region Initialization Methods

	protected override void Awake()
	{
		base.Awake();

		MenuManager.HUDSetActive += ShowHUD;
		MenuManager.HUDSetHidden += HideHUD;

		SetMenuActive(this);
	}

	protected new void Start()
	{
		base.Start();

		Player player = GameManager.Instance.PlayerObject.GetComponent<Player>();
		SetPlayer(player);
	}

	#endregion

	protected new void OnDestroy()
	{
		base.OnDestroy();

		MenuManager.HUDSetActive -= ShowHUD;
		MenuManager.HUDSetHidden -= HideHUD;
	}

	void HideHUD()
	{
		if (m_isMenuActive)
			MenuManager.CloseUnclosableMenu(this);
	}

	void ShowHUD()
	{
		MenuManager.OpenMenu(this);
	}

	internal void SetPlayer(Player player)
	{
		//HUDElement[] elements = GetComponentsInChildren<HUDElement>();
		//foreach (var element in elements)
		//{
		//	element.SetPlayer(player);
		//}
	}
}
