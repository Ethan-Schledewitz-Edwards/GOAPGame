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
			if (GameManager.Instance.PlayerObject.TryGetComponent(out PlayerConstructionController constructionController))
			{
				PlayerWorldControllerManager worldControllerManager =
					GameManager.Instance.PlayerObject.GetComponent<PlayerWorldControllerManager>();

				// Only allow this menu to be toggled when in construction mode
				if (worldControllerManager.IsModeActive(constructionController))
					MenuManager.ToggleMenu(this);
			}
		}
	}

	public override void SetMenuActive(bool active)
	{
		base.SetMenuActive(active);
	}
}