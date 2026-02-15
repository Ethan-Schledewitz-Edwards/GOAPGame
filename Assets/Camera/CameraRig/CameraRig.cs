using UnityEngine;

public class CameraRig : MonoBehaviour
{
	[Header("Camera Properties")]
	[SerializeField] private Vector3 defaultOffset;
	[SerializeField] private bool isCenteredOnStart;
	[SerializeField] private bool isWorldSpaceOffset;

	[Space(10), Header("Movement")]
	[SerializeField] private bool isPositionFollowEnabled = true;
	[SerializeField] private bool isPosSmoothingEnabled = true;
	[SerializeField] private float positionSmoothSpeed = 8f;

	[SerializeField] private bool isXPosSmoothed = true;
	[SerializeField] private bool isYPosSmoothed = true;
	[SerializeField] private bool isZPosSmoothed = true;

	[Space(10), Header("Rotation")]
	[SerializeField] private bool isRotationFollowEnabled = true;
	[SerializeField] private bool isRotSmoothingEnabled = true;
	[SerializeField] private float rotationSmoothSpeed = 8f;

	[SerializeField] private bool isXRotEnabled = true;
	[SerializeField] private bool isYRotEnabled = true;
	[SerializeField] private bool isZRotEnabled = true;
	[SerializeField] private bool isXRotSmoothed = true;
	[SerializeField] private bool isYRotSmoothed = true;
	[SerializeField] private bool isZRotSmoothed = true;

	[Space(10), Header("Targets")]
	[SerializeField] private Transform[] m_CameraTargets;

	// Components
	private Camera mainCamera;
	private Vector3 cameraOffset;
	private bool isInitialized;

	private Vector3 m_prevWorldPos;
	private Vector3 m_worldPos;
	private Vector3 m_smoothedPosition;

	private Quaternion m_prevRotation;
	private Quaternion m_rotation;
	private Quaternion m_smoothedRotation;

	#region Initialization Methods

	private void Awake()
	{
		mainCamera = GetComponentInChildren<Camera>();
		transform.parent = null;
	}

	private void Start()
	{
		if (isCenteredOnStart)
		{
			// Find where to aim
			Vector3 rawCenter = GetCenterOfTargets();

			// Transform position first
			Vector3 targetPos = new Vector3();
			if (isWorldSpaceOffset)
				targetPos = rawCenter + defaultOffset + cameraOffset;
			else
			{
				targetPos = rawCenter + (GetForwardOfTargets() * (defaultOffset.z + cameraOffset.z));
				targetPos += Vector3.up * (defaultOffset.y + cameraOffset.y);
				targetPos += Vector3.Cross(Vector3.up, (rawCenter - transform.position).normalized) * (defaultOffset.x + cameraOffset.x);
			}

			// Update camera position
			m_prevWorldPos = targetPos;
			m_worldPos = targetPos;
			transform.position = m_worldPos;

			// Snap camera to face the target
			Vector3 aimDir = (rawCenter - transform.position).normalized;
			transform.rotation = Quaternion.LookRotation(aimDir, Vector3.up);

			m_smoothedPosition = transform.position;
			m_smoothedRotation = transform.rotation;

			isInitialized = true;
		}
	}
	#endregion

	private void LateUpdate()
	{
		float t = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);

		// Interpolated position between physics frames
		Vector3 interpolatedTargetPos = Vector3.Lerp(m_prevWorldPos, m_worldPos, t);
		Quaternion interpolatedTargetRot = Quaternion.Slerp(m_prevRotation, m_rotation, t);

		if (isPositionFollowEnabled)
		{
			if (isPosSmoothingEnabled)
			{
				float smoothFactor = 1f - Mathf.Exp(-positionSmoothSpeed * Time.deltaTime);

				m_smoothedPosition = CalculateNewPosition(m_smoothedPosition, interpolatedTargetPos, smoothFactor);
			}
			else
			{
				m_smoothedPosition = interpolatedTargetPos;
			}

			transform.position = m_smoothedPosition;
		}

		if (isRotationFollowEnabled)
		{
			if (isRotSmoothingEnabled)
			{
				float smoothFactor = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);

				m_smoothedRotation = CalculateNewRotation(m_smoothedRotation, interpolatedTargetRot, smoothFactor);
			}
			else
			{
				m_smoothedRotation = interpolatedTargetRot;
			}

			transform.rotation = m_smoothedRotation;
		}
	}


	private void FixedUpdate()
	{
		if (!isInitialized && isCenteredOnStart)
			return;

		// Find where to aim
		Vector3 rawCenter = GetCenterOfTargets();
		Vector3 aimDir = (rawCenter - m_worldPos).normalized;

		// Update target rotation
		Quaternion targetRot = Quaternion.LookRotation(aimDir, Vector3.up);
		m_prevRotation = m_rotation;
		m_rotation = targetRot;

		// Update target position
		Vector3 targetPos = new Vector3();
		if (isWorldSpaceOffset)
			targetPos = rawCenter + defaultOffset + cameraOffset;
		else
		{
			targetPos = rawCenter + (GetForwardOfTargets() * (defaultOffset.z + cameraOffset.z));
			targetPos += Vector3.up * (defaultOffset.y + cameraOffset.y);
			targetPos += Vector3.Cross(Vector3.up, aimDir) * (defaultOffset.x + cameraOffset.x);
		}

		// Update camera position
		m_prevWorldPos = m_worldPos;
		m_worldPos = targetPos;
	}

	#region Targeting

	private Vector3 GetCenterOfTargets()
	{
		if (m_CameraTargets.Length == 0)
		{
			return transform.position;
		}

		float x = 0f;
		float y = 0f;
		float z = 0f;

		int targetCount = m_CameraTargets.Length;

		for (int i = 0; i < targetCount; i++)
		{
			x += m_CameraTargets[i].transform.position.x;
			y += m_CameraTargets[i].transform.position.y;
			z += m_CameraTargets[i].transform.position.z;
		}

		x = x / targetCount;
		y = y / targetCount;
		z = z / targetCount;

		return new Vector3(x, y, z);
	}

	private Vector3 GetForwardOfTargets()
	{
		if (m_CameraTargets.Length == 0)
		{
			return transform.forward;
		}

		Vector3 sumOfForwards = Vector3.zero;

		int targetCount = m_CameraTargets.Length;
		for (int i = 0; i < m_CameraTargets.Length; i++)
		{
			sumOfForwards += m_CameraTargets[i].transform.forward;
		}

		Vector3 averageForward = sumOfForwards / m_CameraTargets.Length;
		return averageForward;
	}

	#endregion

	#region Transformation Handling

	private Quaternion CalculateNewRotation(Quaternion prevRot, Quaternion targetRot, float smoothFactor)
	{

		// Per-axis smoothing on Euler angles
		Vector3 prevEuler = prevRot.eulerAngles;
		Vector3 targetEuler = targetRot.eulerAngles;

		Vector3 finalEuler = new Vector3();

		if (isXRotEnabled)
		{
			if (isXRotSmoothed)
				finalEuler.x = Mathf.LerpAngle(prevEuler.x, targetEuler.x, smoothFactor);
			else
				finalEuler.x = targetEuler.x;
		}
		else finalEuler.x = prevEuler.x;

		if (isYRotEnabled)
		{
			if (isYRotSmoothed)
				finalEuler.y = Mathf.LerpAngle(prevEuler.y, targetEuler.y, smoothFactor);
			else
				finalEuler.y = targetEuler.y;
		}
		else finalEuler.y = prevEuler.y;

		if (isZRotEnabled)
		{
			if (isZRotSmoothed)
				finalEuler.z = Mathf.LerpAngle(prevEuler.z, targetEuler.z, smoothFactor);
			else
				finalEuler.z = targetEuler.z;
		}
		else finalEuler.z = prevEuler.z;

		return Quaternion.Euler(finalEuler);
	}

	private Vector3 CalculateNewPosition(Vector3 prevPos, Vector3 targetPos, float smoothFactor)
	{
		// Per-axis smoothing
		Vector3 finalPos = prevPos;

		if (isXPosSmoothed)
			finalPos.x = Mathf.Lerp(prevPos.x, targetPos.x, smoothFactor);
		else
			finalPos.x = targetPos.x;

		if (isYPosSmoothed)
			finalPos.y = Mathf.Lerp(prevPos.y, targetPos.y, smoothFactor);
		else
			finalPos.y = targetPos.y;

		if (isZPosSmoothed)
			finalPos.z = Mathf.Lerp(prevPos.z, targetPos.z, smoothFactor);
		else
			finalPos.z = targetPos.z;

		return finalPos;
	}

	#endregion

	public Transform GetCameraTransform()
	{
		return mainCamera.transform;
	}

	public void SetCameraOffset(Vector3 newOffset) => cameraOffset = newOffset;

	public Vector3 GetCurrentCameraOffset() { return cameraOffset; }
}
