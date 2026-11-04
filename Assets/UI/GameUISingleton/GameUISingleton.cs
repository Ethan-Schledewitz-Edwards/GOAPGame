using UISystem.ConstructionMenu;
using UISystem.Core;
using UISystem.HUD;
using UISystems.PauseMenu;
using UnityEngine;
using UnityEngine.Assertions;

namespace UISystem.PlayerUI
{
	// NOTE: I remember from SBTD that navigating the object heirarchy to find the
	// right component was a pain. It may be easier to try to combine these components
	// at the top in some way, but I'm not quite sure.
	[RequireComponent(typeof(AudioSource))]
	public class GameUISingleton : MonoBehaviour
	{
		// Signleton
		public static GameUISingleton Instance { get; private set; }

		[field: Header("UI Elements")]
		[field: SerializeField] public GameHUD HUD { get; private set; }
		[field: SerializeField] public PauseMenu PauseMenu { get; private set; }
		[field: SerializeField] public ConstructionMenuController ConstructionMenu { get; private set; }

		private void Awake()
		{
			Instance = this;

			Assert.IsNotNull(PauseMenu);
			Assert.IsNotNull(HUD);
			Assert.IsNotNull(ConstructionMenu);
		}

		protected void Start()
		{
			MenuManager.OpenMenu(HUD);
		}
	}
}