using UnityEngine;
using UnityEngine.InputSystem;

public abstract class PlayerWorldControllerBase : MonoBehaviour, IInputHandler
{
	private const float c_cursorSmoothingController = 20.0f;
	private const float c_cursorSmoothingMouse = 30.0f;
	private const float c_maxCursorDistance = 5.0f;

	[SerializeField] private Camera m_mainCamera;
	[SerializeField] protected PlayerCursorVisualizer m_cursorVisualizer;

	private LayerMask m_groundLayer;

	private Vector2 m_rawLookInput;
	private bool m_isUsingMouse;

	private Vector3 m_cursorLocalPosition;
	private Vector3 m_targetCursorWorldPosition;
	protected Vector3 m_cursorWorldPosition;

	protected virtual void Awake()
	{
		if (m_groundLayer == 0) 
			m_groundLayer = LayerMask.GetMask("Default");

		((IInputHandler)this).SetControlsSubscription(true);
	}

	private void OnDisable()
	{
		((IInputHandler)this).SetControlsSubscription(false);
	}

	#region Initialize

	public void Subscribe()
	{
		InputManager.Controls.Player.Look.performed += OnLookInput;
		InputManager.Controls.Player.Look.canceled += OnLookInput;

		InputManager.Controls.Player.Primary.performed += OnPrimaryFireInput;
		InputManager.Controls.Player.Primary.canceled += OnPrimaryFireInput;

		InputManager.Controls.Player.Secondary.performed += OnSecondaryFireInput;
		InputManager.Controls.Player.Secondary.canceled += OnSecondaryFireInput;

		InputManager.Controls.Player.Cycle.performed += OnCycleInput;
	}

	public void UnSubscribe()
	{
		InputManager.Controls.Player.Look.performed -= OnLookInput;
		InputManager.Controls.Player.Look.canceled -= OnLookInput;

		InputManager.Controls.Player.Primary.performed -= OnPrimaryFireInput;
		InputManager.Controls.Player.Primary.canceled -= OnPrimaryFireInput;

		InputManager.Controls.Player.Secondary.performed -= OnSecondaryFireInput;
		InputManager.Controls.Player.Secondary.canceled -= OnSecondaryFireInput;

		InputManager.Controls.Player.Cycle.performed -= OnCycleInput;
	}

	private void OnLookInput(InputAction.CallbackContext context)
	{
		m_isUsingMouse = context.control.device is Pointer;
		m_rawLookInput = context.ReadValue<Vector2>();
	}

	protected abstract void OnPrimaryFireInput(InputAction.CallbackContext context);

	protected abstract void OnSecondaryFireInput(InputAction.CallbackContext context);

	protected abstract void OnCycleInput(InputAction.CallbackContext context);
	#endregion

	private void Update()
	{
		RefreshCursor();
	}

	protected virtual void RefreshCursor()
	{
		Vector3 targetWorldPositionFlattened = transform.position;

		if (m_isUsingMouse)
		{
			Ray mouseRay = m_mainCamera.ScreenPointToRay(m_rawLookInput);
			if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 100f, m_groundLayer))
			{
				Vector3 playerToMouse = mouseHit.point - transform.position;
				playerToMouse.y = 0; // Flatten the direction vector

				// Clamp the direction vector to max radius, then add it back to player position
				Vector3 clampedOffset = Vector3.ClampMagnitude(playerToMouse, c_maxCursorDistance);
				targetWorldPositionFlattened = transform.position + clampedOffset;
			}
		}
		else
		{
			Vector3 stickInput = new Vector3(m_rawLookInput.x, 0, m_rawLookInput.y);
			targetWorldPositionFlattened = transform.position + (stickInput * c_maxCursorDistance);
		}

		// Raycast down from cursor offset
		Vector3 groundRayStart = targetWorldPositionFlattened + (Vector3.up * 20f);
		m_targetCursorWorldPosition = targetWorldPositionFlattened;
		Quaternion targetRotation = Quaternion.identity;

		if (Physics.Raycast(groundRayStart, Vector3.down, out RaycastHit hitData, 40f, m_groundLayer))
		{
			m_targetCursorWorldPosition = hitData.point;

			// Match cursor rotation to ground surface normal
			targetRotation = Quaternion.FromToRotation(Vector3.up, hitData.normal);
		}

		float cursorSmoothing = m_isUsingMouse ? c_cursorSmoothingMouse : c_cursorSmoothingController;
		m_cursorWorldPosition = Vector3.Lerp(m_cursorWorldPosition, m_targetCursorWorldPosition, cursorSmoothing * Time.deltaTime);

		// Update Visuals
		if (m_cursorVisualizer != null)
		{
			Quaternion cursorRotation = m_cursorVisualizer.transform.rotation;
			Quaternion smoothedRotation = Quaternion.Slerp(cursorRotation, targetRotation, cursorSmoothing * Time.deltaTime);

			m_cursorVisualizer.SetVisualsPosition(m_cursorWorldPosition);
			m_cursorVisualizer.SetVisualsRotation(smoothedRotation);
		}
	}
}
