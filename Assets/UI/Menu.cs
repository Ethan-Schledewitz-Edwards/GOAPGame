#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public abstract class Menu : UIElement
{
	[field: Header("Menu Buttons")]
	[SerializeField] private MenuButton[] m_menuButtons;

	// System
	public virtual bool IsUnclosable => false;
	public virtual bool IsHUD => false; // HUD menus don't block camera control
	public virtual bool IsSubPauseMenu => false; // For menus like "Settings" that can enable in the pause menu

	protected bool m_isMenuActive;


	protected virtual void Start()
	{
		if (m_canvasGroup == null)
		{
			Debug.LogError("Null canvas group: " + transform.name);
			return;
		}

		foreach (MenuButton i in m_menuButtons)
		{
			i.InitButton(this);
		}
	}

	protected virtual void OnDestroy()
	{
		bool playing = true;

#if UNITY_EDITOR
		playing = EditorApplication.isPlayingOrWillChangePlaymode;
#endif

		if (m_isMenuActive && playing)
			MenuManager.CloseUnclosableMenu(this);
	}

	public virtual void SetMenuActive(bool active)
	{
		m_isMenuActive = active;

		m_canvasGroup.alpha = active ? 1 : 0;
		m_canvasGroup.interactable = active ? true : false;
		m_canvasGroup.blocksRaycasts = active ? true : false;
	}
}