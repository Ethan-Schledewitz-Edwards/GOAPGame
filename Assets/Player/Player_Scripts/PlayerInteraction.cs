using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour, IInputHandler
{
	// Constants
	private const float k_selectionRadius = 5.0f;

	private Camera m_mainCamera;
	private Vector2 m_mousePosition;

	[SerializeField] private LayerMask m_cursorBlockingLayers;
	[SerializeField] private LayerMask m_actorLayers;
	[SerializeField] private Transform m_cursorVisualizer;

	#region Initialization Methods

	public void Start()
	{
		m_mainCamera = Camera.main;

		((IInputHandler)this).SetControlsSubscription(true);
	}

	private void OnDestroy()
	{
		((IInputHandler)this).SetControlsSubscription(false);
	}
	#endregion

	#region Input Methods

	public void Subscribe()
	{
		InputManager.Controls.Player.Look.performed += OnMouseInput;
		InputManager.Controls.Player.Primary.performed += OnPrimaryInput;
		InputManager.Controls.Player.Secondary.performed += OnSecondaryInput;
	}

	public void UnSubscribe()
	{
		InputManager.Controls.Player.Look.performed -= OnMouseInput;
		InputManager.Controls.Player.Primary.performed -= OnPrimaryInput;
		InputManager.Controls.Player.Secondary.performed -= OnSecondaryInput;
	}

	private void OnMouseInput(InputAction.CallbackContext context)
	{
		m_mousePosition = context.ReadValue<Vector2>();
	}

	private void OnPrimaryInput(InputAction.CallbackContext context)
	{
		Select(m_cursorVisualizer.position);
	}

	private void OnSecondaryInput(InputAction.CallbackContext context)
	{
		TryThrow(m_cursorVisualizer.position);
	}
	#endregion

	private void Update()
	{
		Ray ray = m_mainCamera.ScreenPointToRay(m_mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitData, 100f, m_cursorBlockingLayers))
		{
			m_cursorVisualizer.position = hitData.point;
		}
	}

	private void Select(Vector3 position)
	{
		// Try to select actors
		Collider[] hitColliders = Physics.OverlapSphere(position, k_selectionRadius, m_actorLayers);

		if (hitColliders.Length != 0)
		{
			Debug.Log("Found something!");
		}
	}

	private void TryThrow(Vector3 position)
	{

	}
}
