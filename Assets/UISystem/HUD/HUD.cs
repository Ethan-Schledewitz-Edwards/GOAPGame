using UnityEngine;
using Player.Core;

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

		PlayerEntity player = GameManager.Instance.PlayerObject.GetComponent<PlayerEntity>();
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

	internal void SetPlayer(PlayerEntity player)
	{
		HUDElement[] elements = GetComponentsInChildren<HUDElement>();
		foreach (var element in elements)
		{
			element.SetPlayer(player);
		}
	}
}
