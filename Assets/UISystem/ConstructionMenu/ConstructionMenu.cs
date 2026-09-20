using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConstructionMenu : Menu, IInputHandler
{
	[Header("Sounds")]
	[SerializeField] AudioClip m_openSound;
	[SerializeField] AudioClip m_closeSound;

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void Start()
	{
		base.Start();
		((IInputHandler)this).SetControlsSubscription(true);
		SetMenuActive(false);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		((IInputHandler)this).SetControlsSubscription(false);
	}

	public void Subscribe()
	{
		InputManager.Controls.Permanents.Inventory.performed += OnToggleInput;
	}

	public void UnSubscribe()
	{
		InputManager.Controls.Permanents.Inventory.performed -= OnToggleInput;
	}

	private void OnToggleInput(InputAction.CallbackContext ctx)
	{
		Debug.Log("WTF BRO!!");

		if (MenuManager.MenuCount == 1 || MenuManager.IsMenuOpen(this))
		{
			MenuManager.ToggleMenu(this);
		}
	}

	public override void SetMenuActive(bool active)
	{
		base.SetMenuActive(active);
	}
}