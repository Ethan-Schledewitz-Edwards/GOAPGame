using UnityEngine;

public abstract class TrackingUIElementBase : UIElement
{
	[Header("Offset")]
	[SerializeField] private Vector2 m_localOffset;

	[Header("System")]
	private bool m_canUpdatePosition = false;
	private Vector3 m_followPosition;
	private Vector3 m_anchorPosition;

	private void LateUpdate()
	{
		if (m_canUpdatePosition)
			UpdatePosition(m_followPosition);
	}

	public void SetTrackingPosition(Vector3 position)
	{
		m_followPosition = position;
		m_canUpdatePosition = true;
	}

	public void ResetDynamicPosition()
	{
		m_canUpdatePosition = false;
		m_followPosition = Vector3.zero;
	}

	private void UpdatePosition(Vector3 targetPosition)
	{
		if(m_rectTransform != null &&
			m_parentRectTransform != null)
		{
			Camera camera = Camera.main;
			Vector2 screenPosition = camera.WorldToScreenPoint(targetPosition);
			m_rectTransform.position = screenPosition + m_localOffset;
			m_anchorPosition = screenPosition;
		}
	}

	public abstract void ElementReleased();

	public abstract void ElementReturned();
}
