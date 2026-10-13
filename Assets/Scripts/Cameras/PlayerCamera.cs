using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
	private const float c_zoomSmoothing = 4f;
	private const float c_smoothing = 20f;

	[Header("Camera Settings")]
	[SerializeField] private Vector3 m_defaultOffset = new Vector3(0f, 0.5f, 12f);
	[SerializeField] private Quaternion m_defaultRotation = Quaternion.Euler(50f, 0, 0f);
	[SerializeField] private float m_zoomPerExtraTarget = 2f;
	[SerializeField] private float m_maxZoomOut = 15f;

	[Space(10), Header("Targeting")]
	[SerializeField] private Transform[] m_cameraTargets;

	// Components
	public Camera MainCamera { get; private set; }
	private Vector3 m_cameraOffset;
	private bool m_isInitialized;

	private Vector3 m_position;
	private Vector3 m_smoothedPosition;

	float m_targetCameraZoom;
	float m_cameraZoom;

	#region Initialization Methods

	private void Awake()
	{
		MainCamera = GetComponentInChildren<Camera>();
	}

	private void Start()
	{
		if (m_cameraTargets.Length == 0)
			return;

		MainCamera.transform.rotation = m_defaultRotation;

		Vector3 rawCenter = GetCenterOfTargets();
		UpdateZoomTarget();
		m_cameraZoom = m_targetCameraZoom;

		Vector3 localOffset = new Vector3(m_defaultOffset.x, m_defaultOffset.y, m_defaultOffset.z + m_cameraZoom);
		Vector3 worldOffset = MainCamera.transform.rotation * localOffset;

		m_smoothedPosition = rawCenter + worldOffset + m_cameraOffset;
		transform.position = m_smoothedPosition;

		m_isInitialized = true;
		transform.parent = null;
	}
	#endregion

	private void LateUpdate()
	{
		if (!m_isInitialized || m_cameraTargets.Length == 0)
			return;

		MainCamera.transform.rotation = m_defaultRotation;

		UpdateZoomTarget();

		Vector3 rawCenter = GetCenterOfTargets();

		m_smoothedPosition = CalculateNewPosition(m_smoothedPosition, rawCenter);
		transform.position = m_smoothedPosition;
	}

	/// <summary>
	/// Calculates and sets the target camera zoom based on the maximum distance between camera targets.
	/// </summary>
	private void UpdateZoomTarget()
	{
		int targetCount = m_cameraTargets.Length;
		if (targetCount <= 1)
		{
			m_targetCameraZoom = 0f;
			return;
		}

		// Calculate maximum spread between targets to dynamically zoom out
		float maxDistance = 0f;
		for (int i = 0; i < targetCount; i++)
		{
			for (int j = i + 1; j < targetCount; j++)
			{
				float dist = (m_cameraTargets[i].position - m_cameraTargets[j].position).magnitude;
				if (dist > maxDistance)
				{
					maxDistance = dist;
				}
			}
		}

		m_targetCameraZoom = Mathf.Min(maxDistance * m_zoomPerExtraTarget, m_maxZoomOut);
	}

	private Vector3 CalculateNewPosition(Vector3 prevPos, Vector3 rawCenter)
	{
		m_cameraZoom = Mathf.Lerp(m_cameraZoom, m_targetCameraZoom, c_zoomSmoothing * Time.deltaTime);

		Vector3 localOffset = new Vector3(m_defaultOffset.x, m_defaultOffset.y, -m_defaultOffset.z - m_cameraZoom);
		Vector3 targetWorldPos = rawCenter + (MainCamera.transform.rotation * localOffset);

		Vector3 camRight = MainCamera.transform.right;
		Vector3 camForward = MainCamera.transform.forward;

		camRight.y = 0f;
		camForward.y = 0f;
		camRight.Normalize();
		camForward.Normalize();

		Vector3 flattenedOffset = (camRight * m_cameraOffset.x) + (camForward * m_cameraOffset.y);
		targetWorldPos += flattenedOffset;

		Vector3 finalPos = prevPos;
		finalPos.x = Mathf.Lerp(prevPos.x, targetWorldPos.x, c_smoothing * Time.deltaTime);
		finalPos.y = Mathf.Lerp(prevPos.y, targetWorldPos.y, c_smoothing * Time.deltaTime);
		finalPos.z = Mathf.Lerp(prevPos.z, targetWorldPos.z, c_smoothing * Time.deltaTime);

		return finalPos;
	}

	public void SetCameraOffset(Vector3 newOffset) => m_cameraOffset = newOffset;

	public Transform GetCameraTransform() => MainCamera.transform;

	private Vector3 GetCenterOfTargets()
	{
		if (m_cameraTargets.Length == 0)
		{
			return transform.position;
		}

		float x = 0f;
		float y = 0f;
		float z = 0f;

		int targetCount = m_cameraTargets.Length;

		for (int i = 0; i < targetCount; i++)
		{
			x += m_cameraTargets[i].transform.position.x;
			y += m_cameraTargets[i].transform.position.y;
			z += m_cameraTargets[i].transform.position.z;
		}

		x = x / targetCount;
		y = y / targetCount;
		z = z / targetCount;

		return new Vector3(x, y, z);
	}
}