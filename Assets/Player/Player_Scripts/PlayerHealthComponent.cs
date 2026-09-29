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
	}
}