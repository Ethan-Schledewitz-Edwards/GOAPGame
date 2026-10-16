using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Core
{
	public class PlayerCameraController : MonoBehaviour, IInputHandler
	{
		private const float c_lookaheadSmoothingMouse = 8f;
		private const float c_lookaheadSmoothingController = 10f;
		private const float c_lookaheadSmoothingMovement = 1.2f;

		private const float c_lookaheadRangeMovement = 1.2f;
		private const float c_lookaheadRangeAiming = 4f;

		[Header("Mouse Look Settings")]
		[SerializeField, Range(0f, 1f)] private float m_innerRadius = 0.4f; // Deadzone radius (fraction of screen size)
		[SerializeField, Range(0f, 1f)] private float m_outerRadius = 0.8f; // Max speed radius (fraction of screen size)

		private PlayerCamera m_playerCamera;

		private Vector2 m_movementDirection;

		private bool m_isUsingMouse;
		private bool m_isAiming;
		private Vector2 m_lookInput;

		private Vector2 m_cameraLookahead = Vector2.zero;
		private Vector2 m_cameraAim = Vector2.zero;
		private Vector2 m_cameraOffset;

		private void Awake()
		{
			m_playerCamera = GetComponentInChildren<PlayerCamera>();
		}

		private void Start()
		{
			((IInputHandler)this).SetControlsSubscription(true);
		}

		private void OnDestroy()
		{
			((IInputHandler)this).SetControlsSubscription(false);
		}

		#region Input Handlers

		private void OnLookInput(InputAction.CallbackContext context)
		{
			m_isUsingMouse = context.control.device is Pointer;
			m_lookInput = context.ReadValue<Vector2>().normalized;
		}

		private void OnAimInput(InputAction.CallbackContext context)
		{
			m_isAiming = context.ReadValueAsButton();
		}

		public void Subscribe()
		{
			InputManager.Controls.Player.Look.performed += OnLookInput;
			InputManager.Controls.Player.Look.canceled += OnLookInput;

			InputManager.Controls.Player.Secondary.performed += OnAimInput;
			InputManager.Controls.Player.Secondary.canceled += OnAimInput;
		}

		public void UnSubscribe()
		{
			InputManager.Controls.Player.Look.performed -= OnLookInput;
			InputManager.Controls.Player.Look.canceled -= OnLookInput;

			InputManager.Controls.Player.Secondary.performed += OnAimInput;
			InputManager.Controls.Player.Secondary.canceled += OnAimInput;
		}
		#endregion

		private void Update()
		{
			UpdateMovementLookahead();
			UpdateCursorPosition(out Vector2 cameraAim);

			if (m_playerCamera != null)
			{
				m_playerCamera.SetCameraOffset(cameraAim);
			}
		}

		public void SetMovementDirection(Vector2 direction)
		{
			m_movementDirection = direction.magnitude > 0.01f ? direction.normalized : Vector2.zero;
		}

		private void UpdateMovementLookahead()
		{
			Vector2 targetInput = (m_isAiming) ? Vector2.zero : m_movementDirection;

			Vector3 targetLookahead = new Vector3(targetInput.x, targetInput.y, 0) * c_lookaheadRangeMovement;
			m_cameraLookahead = Vector3.Lerp(m_cameraLookahead, targetLookahead, c_lookaheadSmoothingMovement * Time.deltaTime);

			if (m_cameraLookahead.magnitude <= 0.05f)
				m_cameraLookahead = Vector2.zero;
		}

		private void UpdateCursorPosition(out Vector2 cameraOffset)
		{
			cameraOffset = m_cameraAim;

			if (m_playerCamera == null ||
				InputManager.ControlMode != InputManager.ControlType.Player)
				return;

			Vector2 lookInput = Vector2.zero;

			// Find mouse position on screen
			if (m_isUsingMouse && m_isAiming)
			{
				Vector2 mousePosition = Mouse.current.position.ReadValue();
				Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

				float normX = (mousePosition.x - screenCenter.x) / Screen.width;
				float normY = (mousePosition.y - screenCenter.y) / Screen.height;

				Vector2 ovalDelta = new Vector2(normX * 2f, normY * 2f);
				float distFromCenter = ovalDelta.magnitude;

				if (distFromCenter > m_innerRadius)
				{
					float intensity = Mathf.InverseLerp(m_innerRadius, m_outerRadius, distFromCenter);
					intensity = Mathf.Clamp01(intensity);

					lookInput = ovalDelta.normalized * intensity;
				}
			}
			else if (!m_isUsingMouse) // Use stick delta
			{
				lookInput = m_lookInput;
			}

			Vector3 targetAim = new Vector3(lookInput.x, lookInput.y, 0) * c_lookaheadRangeAiming;

			float offsetSmoothing = m_isUsingMouse ? c_lookaheadSmoothingMouse : c_lookaheadSmoothingController;
			m_cameraAim = Vector3.Lerp(m_cameraAim, targetAim, offsetSmoothing * Time.deltaTime);
			if (m_cameraAim.magnitude <= 0.05f)
				m_cameraAim = Vector2.zero;

			cameraOffset = m_cameraAim;
		}
	}
}
