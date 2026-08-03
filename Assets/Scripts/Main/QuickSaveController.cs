using UnityEngine;
using UnityEngine.InputSystem;

namespace Main.Core
{
	[RequireComponent(typeof(GameSaveCoordinator))]
    public class QuickSaveController : MonoBehaviour, IInputHandler
    {
		GameSaveCoordinator m_saveCoordinator;

		private void Awake()
		{
			m_saveCoordinator = GetComponent<GameSaveCoordinator>();

			((IInputHandler)this).SetControlsSubscription(true);
		}

		private void OnDestroy()
		{
			((IInputHandler)this).SetControlsSubscription(false);
		}

		public void Subscribe()
		{
			InputManager.Controls.Permanents.QuickSave.performed += OnQuickSaveInput;
			InputManager.Controls.Permanents.QuickLoad.performed += OnQuickLoadInput;
		}

		public void UnSubscribe()
		{
			InputManager.Controls.Permanents.QuickSave.performed -= OnQuickSaveInput;
			InputManager.Controls.Permanents.QuickLoad.performed -= OnQuickLoadInput;
		}

		private void OnQuickSaveInput(InputAction.CallbackContext context)
		{
			m_saveCoordinator?.SaveGame();
		}

		private void OnQuickLoadInput(InputAction.CallbackContext context)
		{
			m_saveCoordinator?.LoadGame();
		}
	}
}
