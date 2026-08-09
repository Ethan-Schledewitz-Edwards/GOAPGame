using Player.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Core
{
	[RequireComponent(typeof(PlayerWorldControllerManager))]
	public abstract class PlayerWorldControllerBase : MonoBehaviour, IInputHandler
	{
		protected PlayerWorldControllerManager m_worldControllerManager;

		protected virtual void Awake()
		{
			m_worldControllerManager = GetComponent<PlayerWorldControllerManager>();
		}

		private void OnEnable()
		{
			((IInputHandler)this).SetControlsSubscription(true);
		}

		private void OnDisable()
		{
			((IInputHandler)this).SetControlsSubscription(false);
		}

		#region Initialize

		public void Subscribe()
		{
			InputManager.Controls.Player.Primary.performed += OnPrimaryFireInput;
			InputManager.Controls.Player.Primary.canceled += OnPrimaryFireInput;

			InputManager.Controls.Player.Secondary.performed += OnSecondaryFireInput;
			InputManager.Controls.Player.Secondary.canceled += OnSecondaryFireInput;

			InputManager.Controls.Player.Cycle.performed += OnCycleInput;
		}

		public void UnSubscribe()
		{
			InputManager.Controls.Player.Primary.performed -= OnPrimaryFireInput;
			InputManager.Controls.Player.Primary.canceled -= OnPrimaryFireInput;

			InputManager.Controls.Player.Secondary.performed -= OnSecondaryFireInput;
			InputManager.Controls.Player.Secondary.canceled -= OnSecondaryFireInput;

			InputManager.Controls.Player.Cycle.performed -= OnCycleInput;
		}

		protected abstract void OnPrimaryFireInput(InputAction.CallbackContext context);

		protected abstract void OnSecondaryFireInput(InputAction.CallbackContext context);

		protected abstract void OnCycleInput(InputAction.CallbackContext context);
		#endregion

		public abstract void RefreshCursor(Vector3 worldPosition);
	}
}