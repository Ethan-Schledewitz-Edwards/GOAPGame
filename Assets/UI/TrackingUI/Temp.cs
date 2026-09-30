using UnityEngine;

public class Temp : MonoBehaviour
{
    [SerializeField] private TrackingUIElement m_trackingUIElement;

    void Start()
    {
		m_trackingUIElement.SetTrackingPosition(transform.position);
	}

	private void Update()
	{
		m_trackingUIElement.SetTrackingPosition(transform.position);
	}
}
