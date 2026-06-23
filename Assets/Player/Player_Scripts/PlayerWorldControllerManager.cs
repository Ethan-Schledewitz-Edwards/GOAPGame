using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWorldControllerManager : MonoBehaviour, IInputHandler
{
	public static PlayerWorldControllerBase CurrentWorldController;

	[field: SerializeField] public PlayerCursorVisualizer CursorVisualizer { get; private set; }
	[field: SerializeField] public Camera MainCamera { get; private set; }
	[SerializeField] private PlayerFollowerController m_followerController;
	[SerializeField] private PlayerConstructionController m_constructionController;

	public event Action<PlayerWorldControllerBase> ControlModeChanged;


	private void Awake()
	{
		m_followerController.InitializeController(this);
		m_constructionController.InitializeController(this);
	}

	private IEnumerator Start()
	{
		((IInputHandler)this).SetControlsSubscription(true);

		yield return null;
		yield return null;

		SetCurrentInteractionController(m_followerController);
	}

	private void OnDisable()
	{
		((IInputHandler)this).SetControlsSubscription(false);
	}

	#region Input Methods

	public void Subscribe()
	{
		InputManager.Controls.Player.Look.performed += OnMouseInput;

		InputManager.Controls.Player.Primary.performed += OnPrimaryInput;
		InputManager.Controls.Player.Primary.canceled += OnPrimaryInput;

		InputManager.Controls.Player.Secondary.performed += OnSecondaryInput;
		InputManager.Controls.Player.Secondary.canceled += OnSecondaryInput;

		InputManager.Controls.Player.CommandMode.performed += OnFollowerControllerInput;
		InputManager.Controls.Player.BuildMode.performed += OnConstructionControllerInput;

		InputManager.Controls.Player.Cycle.performed += OnCycleInput;
	}

	public void UnSubscribe()
	{
		InputManager.Controls.Player.Look.performed -= OnMouseInput;

		InputManager.Controls.Player.Primary.performed -= OnPrimaryInput;
		InputManager.Controls.Player.Primary.canceled -= OnPrimaryInput;

		InputManager.Controls.Player.Secondary.performed -= OnSecondaryInput;
		InputManager.Controls.Player.Secondary.canceled -= OnSecondaryInput;

		InputManager.Controls.Player.CommandMode.performed -= OnFollowerControllerInput;
		InputManager.Controls.Player.BuildMode.performed -= OnConstructionControllerInput;

		InputManager.Controls.Player.Cycle.performed -= OnCycleInput;
	}

	private void OnMouseInput(InputAction.CallbackContext context)
		=> CurrentWorldController?.SetMouseScreenPosition(context.ReadValue<Vector2>());

	private void OnPrimaryInput(InputAction.CallbackContext context)
		=> CurrentWorldController?.PrimaryFire(context);

	private void OnSecondaryInput(InputAction.CallbackContext context)
		=> CurrentWorldController?.SecondaryFire(context);

	private void OnCycleInput(InputAction.CallbackContext context)
	{
		int direction = Math.Sign(context.ReadValue<float>());
		CurrentWorldController?.Cycle(direction);
	}

	private void OnFollowerControllerInput(InputAction.CallbackContext context)
		=> SetCurrentInteractionController(m_followerController);

	private void OnConstructionControllerInput(InputAction.CallbackContext context)
		=> SetCurrentInteractionController(m_constructionController);

	#endregion

	public void SetCurrentInteractionController(PlayerWorldControllerBase interactionControllerBase)
	{
		if (interactionControllerBase == null)
			return;

		if(CurrentWorldController != null)
			CurrentWorldController.OnControllerDisabled();

		CurrentWorldController = interactionControllerBase;
		CurrentWorldController.OnControllerEnabled();
		ControlModeChanged?.Invoke(CurrentWorldController);
	}

	public static bool IsModeActive(PlayerWorldControllerBase controllerBase)
	{
		return CurrentWorldController == controllerBase;
	}
}
