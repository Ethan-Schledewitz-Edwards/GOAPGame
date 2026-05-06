using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class Player : Entity
{
	[field: SerializeField] public CameraRig PlayerCamera { get; private set; }
	[field: SerializeField] public Transform PlayerMesh { get; private set; }
	public PlayerController PlayerController { get; private set; }

	protected override void Awake()
	{
		base.Awake();

		PlayerController = GetComponent<PlayerController>();
	}

	protected override IEnumerator TrySpawn()
	{
		m_isSpawning = true;

		float startHeight = 2f;
		float rayLength = 10f;
		Vector3 rayStart = transform.position + (Vector3.up * startHeight);
		RaycastHit hit;

		// Raycast down to find the highest possible point
		while (!Physics.Raycast(rayStart, Vector3.down * rayLength, out hit, rayLength, m_collisionLayerMask, QueryTriggerInteraction.Ignore) && m_isSpawning)
			yield return null;

		PlayerController.Teleport(hit.point);
		m_isSpawning = false;
	}
}
