using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class BuildMenu : Menu
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

	#region Input Methods

	private void OnToggleInput(InputAction.CallbackContext ctx)
	{
		if(MenuManager.PauseMenu)

		MenuManager.ToggleMenu(this);
	}
	#endregion

	#region UI State

	public override void SetMenuActive(bool active)
	{
		base.SetMenuActive(active);
	}

	#endregion
}