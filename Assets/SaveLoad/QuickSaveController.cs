using UnityEngine;
using UnityEngine.InputSystem;

namespace SaveLoad.Management
{
    public class QuickSaveController : MonoBehaviour, IInputHandler
    {
		SaveManager m_saveManager;

		private void Awake()
		{
			m_saveManager = GetComponent<SaveManager>();

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
			m_saveManager?.SaveGame();
		}

		private void OnQuickLoadInput(InputAction.CallbackContext context)
		{
			m_saveManager?.LoadGame();
		}
	}
}
