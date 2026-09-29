using Entities.Core;
using SaveLoad.Core;
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

			if (GetComponent<ISavableEntity>() != null)
				GetComponent<ISavableEntity>().TransformRestored += HandleSpawned;
		}

		private void OnDestroy()
		{
			if (GetComponent<ISavableEntity>() != null)
				GetComponent<ISavableEntity>().TransformRestored -= HandleSpawned;

			StopAllCoroutines();
		}

		private void HandleSpawned(Vector3 savedPosition, Quaternion savedRotation)
		{
			m_player.PlayerController.Teleport(savedPosition);
		}
	}
}