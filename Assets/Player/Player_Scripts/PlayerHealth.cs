using System;
using System.Collections;
using UnityEngine;

namespace Player.Core
{
	public class PlayerHealth : HealthBase
	{
		// Components
		private PlayerEntity m_player;

		// Events
		public event Action<int> HealthChanged;

		protected override void Awake()
		{
			base.Awake();

			m_player = GetComponent<PlayerEntity>();
		}

		public override void SetHealth(int newHealthValue)
		{
			base.SetHealth(newHealthValue);

			HealthChanged?.Invoke(newHealthValue);
		}
	}
}