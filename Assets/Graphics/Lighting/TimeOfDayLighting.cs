using Managers;
using System.Linq;
using UnityEngine;

namespace WorldLighting
{
	public class TimeOfDayLighting : MonoBehaviour
	{
		private IGameClock m_gameClock;

		[field: Header("Lighting Profile")]
		[field: SerializeField] private LightingProfile m_lightingProfile;

		[Header("Orbit Settings")]
		[SerializeField] private Vector3 m_mapCenter = Vector3.zero;
		[SerializeField] private float m_orbitRadius = 500f;
		[SerializeField] private float m_orbitHeight = 300f;
		[SerializeField] private float m_orbitAngleOffset = -45f; // Adjusts which side the sun rises/sets from

		private void Awake()
		{
			m_gameClock = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
				.OfType<IGameClock>()
				.FirstOrDefault();

			if (m_gameClock == null)
				Debug.LogError($"{gameObject}: {typeof(TimeOfDayLighting)} is missing an {typeof(IGameClock)} reference.");
		}

		private void OnEnable()
		{
			if (m_gameClock != null)
			{
				m_gameClock.TimeOfDayFractionUpdated += UpdateTime;
				UpdateTime(m_gameClock.GetTimeOfDayFraction());
			}
		}

		private void OnDisable()
		{
			if (m_gameClock != null)
			{
				m_gameClock.TimeOfDayFractionUpdated -= UpdateTime;
			}
		}

		public void UpdateTime(float timeOfDayPercentage)
		{
			if (m_lightingProfile != null)
			{
				float sunriseHr = (m_lightingProfile.SunriseHr / 24f);
				float sunsetHr = (m_lightingProfile.SunsetHr / 24f);

				float adjustedTime = NormalizeTime(timeOfDayPercentage, sunriseHr, sunsetHr);
				UpdateLighting(adjustedTime);
			}
		}

		private float NormalizeTime(float time, float sunrise, float sunset)
		{
			if (time >= sunrise && time <= sunset)
			{
				return Mathf.InverseLerp(sunrise, sunset, time) * 0.5f;// Sunrise to Sunset (0 to 0.5)
			}
			else
			{
				float nightDuration = (1f - sunset) + sunrise;
				float timeIntoNight = (time > sunset) ? (time - sunset) : (1f - sunset + time);

				return 0.5f + (timeIntoNight / nightDuration) * 0.5f;
			}
		}

		private void UpdateLighting(float timeOfDayFract)
		{
			Light sun = RenderSettings.sun;
			if (m_lightingProfile != null)
			{
				// Fog
				RenderSettings.fogColor = m_lightingProfile.FogColorOverDay.Evaluate(timeOfDayFract);
				RenderSettings.fogStartDistance = m_lightingProfile.FogStartOverDay.Evaluate(timeOfDayFract);
				RenderSettings.fogEndDistance = m_lightingProfile.FogEndOverDay.Evaluate(timeOfDayFract);

				// Ambient colour
				Color ambientColor = m_lightingProfile.AmbientColorOverDay.Evaluate(timeOfDayFract);
				RenderSettings.subtractiveShadowColor = ambientColor;

				// Sun colour
				Color lightColor = m_lightingProfile.LightingColorOverDay.Evaluate(timeOfDayFract);
				float colorStrength = m_lightingProfile.LightFadeOverDay.Evaluate(timeOfDayFract);
				if (sun != null)
				{
					sun.color = Color.Lerp(ambientColor, lightColor, colorStrength);
				}
			}

			if (sun != null)
			{
				float currentAngle = (timeOfDayFract * 360f) + m_orbitAngleOffset;
				float radians = currentAngle * Mathf.Deg2Rad;

				// Calculate the circle position around the map center
				Vector3 orbitOffset = new Vector3(
					Mathf.Sin(radians) * m_orbitRadius,
					m_orbitHeight,
					Mathf.Cos(radians) * m_orbitRadius
				);

				// Position the directional light in the sky and point it straight at the map center
				sun.transform.position = m_mapCenter + orbitOffset;
				sun.transform.LookAt(m_mapCenter);
			}
		}
	}

}