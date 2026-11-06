using System.Collections;
using System.Linq;
using Math.Core;
using Player.Core;
using UISystem.Core;
using UnityEngine;
using Slider = UnityEngine.UI.Slider;

namespace UISystem.HUD
{
	[RequireComponent(typeof(Slider))]
	public class PlayerHealthBar : UIElement
	{
		[SerializeField] private Slider m_healthSlider;
		[Space]
		[SerializeField] private float m_sliderMoveDuration = 0.5f;
		
		private PlayerHealth m_playerHealth;

		private void Start()
		{
			if (m_healthSlider == null) m_healthSlider = GetComponent<Slider>();

			m_playerHealth = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include)
				.FirstOrDefault();

			m_healthSlider.maxValue = m_playerHealth.MaxHealth;
			m_healthSlider.value = m_playerHealth.MaxHealth;
			m_playerHealth.OnHealthChanged += UpdateSlider;
		}

		private void UpdateSlider(int newHealthValue)
		{
			StopAllCoroutines();
			StartCoroutine(UpdateSliderCoroutine(newHealthValue));
		}

		private IEnumerator UpdateSliderCoroutine(int newValue)
		{
			float startingValue = m_healthSlider.value;
			
			float time = 0;
			float duration = m_sliderMoveDuration;

			while (time < duration)
			{
				time += Time.deltaTime;

				m_healthSlider.value = Mathf.Lerp(startingValue, newValue, EasingFunctions.EaseOutQuart(time / duration));
				
				yield return null;
			}
		}
	}
}
