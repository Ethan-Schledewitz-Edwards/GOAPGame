using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem.HID;

[RequireComponent(typeof(AudioSource))]
public class PlayerUI : MonoBehaviour
{
	// Signleton
	public static PlayerUI Instance { get; private set; }

	public static bool PauseMenuActive { get; private set; }

	[field: Header("UI Elements")]
	[field: SerializeField] public HUD HUD { get; private set; }
	[field: SerializeField] public PauseMenu PauseMenu { get; private set; }

	protected void Awake()
	{
		Instance = this;

		Assert.IsNotNull(PauseMenu);
		Assert.IsNotNull(HUD);
	}

	protected void Start()
	{
		UIElement[] elements = GetComponentsInChildren<UIElement>(includeInactive: true);
		foreach (var element in elements)
		{
			element.m_playerUI = this;
		}

		MenuManager.OpenMenu(HUD);

		//Player.Health.OnGameStateChange += ShowPlayerStateScreen;
	}

	private void OnDestroy()
	{
		//Player.Health.OnGameStateChange -= ShowPlayerStateScreen;
	}
}

