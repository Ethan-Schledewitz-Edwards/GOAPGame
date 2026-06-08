using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///  Keeps track of the number of camera blocking menus open. 
///  Each menu increments the menu count when open and decrements when closed.
/// </summary>
public static class MenuManager
{
	public static AudioSource AudioSource { get; private set; }
	public static Menu PauseMenu;

	// Events
	public static Action MenuUpdated;
	public static event Action HUDSetActive;
	public static event Action HUDSetHidden;

	// System
	static readonly List<Menu> s_openMenus = new();
	public static int MenuCount => s_openMenus.Count;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void ClearStatics()
	{
		s_openMenus.Clear();
		InputManager.Controls.Permanents.Pause.performed += OnPausePressed;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void Init()
	{
		GameObject menuAudio = new("MenuAudio", typeof(AudioSource));
		UnityEngine.Object.DontDestroyOnLoad(menuAudio);
		AudioSource = menuAudio.GetComponent<AudioSource>();
		AudioSource.spatialBlend = 0;
	}

	static void OnPausePressed(InputAction.CallbackContext ctx)
	{
		Menu topClosable = null;
		foreach (var menu in s_openMenus)
		{
			if (!menu.IsUnclosable)
				topClosable = menu;
		}

		// Close topmost menu
		if (topClosable != null)
		{
			CloseMenu(topClosable);
		}
		else
		{
			if (PauseMenu)
				ToggleMenu(PauseMenu);
		}
	}

	public static bool IsMenuOpen(Menu menu)
	{
		return s_openMenus.Contains(menu);
	}

	public static void OpenMenu(Menu menu)
	{
		if (menu == null)
		{
			Debug.LogError("Null menu");
			return;
		}

		if (IsMenuOpen(PauseMenu) && !menu.IsSubPauseMenu)
		{
			Debug.LogError("Tried to open a non sub-pause menu while the pause menu is active.");
			return;
		}

		if (IsMenuOpen(menu))
		{
			Debug.LogWarning($"Trying to double add menu {menu.GetType().Name}");
			return;
		}

		// Hide the HUD when another menu is open
		bool shouldHideHud = s_openMenus.Count > 0 && s_openMenus[0].IsHUD && !menu.IsHUD;

		menu.SetMenuActive(true);
		s_openMenus.Add(menu);

		if (shouldHideHud)
			HUDSetHidden?.Invoke();

		UpdateControlMode();
		MenuUpdated?.Invoke();

		Debug.Log($"Opened {menu.GetType().Name}");
	}

	public static void CloseMenu(Menu menu)
	{
		if (menu.IsUnclosable) return;
		CloseMenu_Internal(menu);
	}

	public static void CloseUnclosableMenu(Menu menu)
	{
		CloseMenu_Internal(menu);
	}

	static void CloseMenu_Internal(Menu menu)
	{
		if (!s_openMenus.Contains(menu))
		{
			Debug.LogWarning($"Trying to double remove menu {menu.GetType().Name}");
			return;
		}

		menu.SetMenuActive(false);
		s_openMenus.Remove(menu);
		UpdateControlMode();
		MenuUpdated?.Invoke();

		// Show HUD if no menus are open
		if (s_openMenus.Count <= 0)
			HUDSetActive?.Invoke();

		Debug.Log($"Closed {menu.GetType().Name}");
	}

	public static void ToggleMenu(Menu menu)
	{
		if (!s_openMenus.Contains(menu))
		{
			OpenMenu(menu);
		}
		else
		{
			CloseMenu(menu);
		}
	}

	static void UpdateControlMode()
	{
		Menu topSolid = null;
		foreach (var menu in s_openMenus)
		{
			if (!menu.IsHUD)
				topSolid = menu;
		}

		if (topSolid != null)
		{
			InputManager.SetControlMode(InputManager.ControlType.UI);
		}
		else
		{
			InputManager.SetControlMode(InputManager.ControlType.Player);
		}
	}
}
