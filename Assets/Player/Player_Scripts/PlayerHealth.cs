using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;

namespace Player.Core
{
	public class PlayerHealth : HealthBase
	{
		// Components
		private PlayerEntity m_player;

		// Events
		public event Action<int> OnHealthChanged;

		protected override void Awake()
		{
			base.Awake();

			m_player = GetComponent<PlayerEntity>();
		}

		public override void SetHealth(int newHealthValue)
		{
			base.SetHealth(newHealthValue);

			OnHealthChanged?.Invoke(newHealthValue);
		}
		
		// TESTING UI ELEMENTS
		[Button]
		private void TakeFiveDamage()
		{
			SetHealth(Health - 5);
		}
		
		[Button]
		private void HealFiveDamage()
		{
			SetHealth(Health + 5);
		}
	}
}