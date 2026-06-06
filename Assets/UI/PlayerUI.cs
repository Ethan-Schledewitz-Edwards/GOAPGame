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
	[field: SerializeField] public BuildMenu BuildMenu { get; private set; }

	protected void Awake()
	{
		Instance = this;

		Assert.IsNotNull(PauseMenu);
		Assert.IsNotNull(HUD);
		Assert.IsNotNull(BuildMenu);
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

