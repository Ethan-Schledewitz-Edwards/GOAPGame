using UnityEngine;

public static class InputManager
{
	public static Controls Controls { get; private set; }
	public static ControlType ControlMode { get; private set; }

	public enum ControlType
	{
		Player,
		UI,
		Disabled
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void ClearStatics()
	{
		ControlMode = ControlType.Player;
		Controls = null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Initialize()
	{
		Controls = new Controls();
		Controls.Enable();
		Controls.Permanents.Enable();
	}

	/// <summary>
	/// Sets the control scheme that determine which inputs are possible.
	/// </summary>
	public static void SetControlMode(ControlType controlType)
	{
		ControlMode = controlType;

		Controls.UI.Disable();
		Controls.Player.Disable();

		switch (controlType)
		{
			case ControlType.Player:
				Controls.Player.Enable();
				break;

			case ControlType.UI:
				Controls.UI.Enable();
				break;

			case ControlType.Disabled:
				break;
		}

		bool controlEnabled = ControlMode == ControlType.Disabled;
		Cursor.lockState = controlEnabled ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !controlEnabled;
	}
}
