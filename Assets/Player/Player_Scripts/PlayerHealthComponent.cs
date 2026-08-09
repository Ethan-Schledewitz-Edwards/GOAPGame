using System.Collections;
using UnityEngine;

namespace Player.Core
{
	public class PlayerHealthComponent : HealthComponent
	{
		private PlayerEntity m_player;

		protected override void Awake()
		{
			base.Awake();

			m_player = GetComponent<PlayerEntity>();
		}

		protected override IEnumerator TrySpawn()
		{
			yield return null;

			m_isSpawning = true;

			float startHeight = 2f;
			float rayLength = 10f;
			Vector3 rayStart = transform.position + (Vector3.up * startHeight);
			RaycastHit hit;

			// Raycast down to find the highest possible point
			while (!Physics.Raycast(rayStart, Vector3.down * rayLength, out hit, rayLength, m_collisionLayerMask, QueryTriggerInteraction.Ignore) && m_isSpawning)
				yield return null;

			m_player.PlayerController.Teleport(hit.point);
			m_isSpawning = false;
		}
	}
}