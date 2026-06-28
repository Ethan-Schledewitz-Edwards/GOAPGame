using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConstructionMenu : Menu
{
	[Header("Sounds")]
	[SerializeField] AudioClip openSound;
	[SerializeField] AudioClip closeSound;

	#region Initialization Methods

	protected override void Awake()
	{
		base.Awake();

		SetMenuActive(false);
	}

	private void OnEnable()
	{
		InputManager.Controls.Permanents.Inventory.performed += OnToggleInput;
	}

	protected virtual void OnDisable()
	{
		InputManager.Controls.Permanents.Inventory.performed -= OnToggleInput;
	}
	#endregion

	private void OnToggleInput(InputAction.CallbackContext ctx)
	{
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