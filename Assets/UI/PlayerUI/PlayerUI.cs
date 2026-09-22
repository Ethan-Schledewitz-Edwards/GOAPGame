using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(AudioSource))]
public class PlayerUI : MonoBehaviour
{
	// Signleton
	public static PlayerUI Instance { get; private set; }

	[field: Header("UI Elements")]
	[field: SerializeField] public HUD HUD { get; private set; }
	[field: SerializeField] public PauseMenu PauseMenu { get; private set; }
	[field: SerializeField] public ConstructionMenu BuildMenu { get; private set; }

	private void Awake()
	{
		Instance = this;

		Assert.IsNotNull(PauseMenu);
		Assert.IsNotNull(HUD);
		Assert.IsNotNull(BuildMenu);
	}

	protected void Start()
	{
		MenuManager.OpenMenu(HUD);
	}
}