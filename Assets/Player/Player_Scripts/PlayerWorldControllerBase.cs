using UnityEngine;

public abstract class PlayerWorldControllerBase : MonoBehaviour, IInputHandler
{
	protected const float c_SelectionRadius = 1.0f;

	protected Camera m_mainCamera;
	protected Vector2 m_mousePosition;

	[SerializeField] protected LayerMask m_cursorBlockingLayers;
	[SerializeField] protected PlayerCursorVisualizer m_cursorVisualizer;

	protected virtual void Start()
	{
		m_mainCamera = Camera.main;
	}

	protected virtual void OnEnable()
	{
		((IInputHandler)this).SetControlsSubscription(true);
	}

	protected virtual void OnDisable()
	{
		((IInputHandler)this).SetControlsSubscription(false);
	}

	public abstract void Subscribe();

	public abstract void UnSubscribe();

	protected virtual void Update()
	{
		Ray ray = m_mainCamera.ScreenPointToRay(m_mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitData, 100f, m_cursorBlockingLayers))
		{
			m_cursorVisualizer.SetPosition(hitData.point);
		}
	}
}
