using UnityEngine;
using UnityEngine.InputSystem;

public abstract class PlayerWorldControllerBase : MonoBehaviour
{
	public abstract string ControllerName { get; }
	public abstract Sprite ControllerIcon { get; }

	protected PlayerWorldControllerManager m_controllerManager;
	private Vector2 m_mouseScreenPosition;
	protected Vector3 m_mouseWorldPosition;

	[SerializeField] private LayerMask m_groundLayer;

	protected virtual void Awake()
	{
		if (m_groundLayer == 0) 
			m_groundLayer = LayerMask.GetMask("Default");
	}

	public void InitializeController(PlayerWorldControllerManager playerWorldControllerManager)
	{
		m_controllerManager = playerWorldControllerManager;
	}

	public abstract void OnControllerEnabled();

	public abstract void OnControllerDisabled();

	public void SetMouseScreenPosition(Vector2 screenPos) => m_mouseScreenPosition = screenPos;

	public abstract void PrimaryFire(InputAction.CallbackContext context);

	public abstract void SecondaryFire(InputAction.CallbackContext context);

	public abstract void Cycle(int cycleDirection);

	private void Update()
	{
		RefreshCursor(out _);
	}

	protected virtual void RefreshCursor(out RaycastHit hitData)
	{
		Ray ray = m_controllerManager.MainCamera.ScreenPointToRay(m_mouseScreenPosition);
		if (Physics.Raycast(ray, out hitData, 100f, m_groundLayer))
		{
			m_mouseWorldPosition = hitData.point;
			m_controllerManager.CursorVisualizer.SetPosition(m_mouseWorldPosition);
		}
	}
}
