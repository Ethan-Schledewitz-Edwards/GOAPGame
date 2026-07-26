using UnityEngine;

namespace WorldLighting
{
	public class TimeOfDayLightingManager : MonoBehaviour
	{
		[field: Header("Lighting Controller")]
		[field: SerializeField] private ITimeOfDayController m_timeOfDayController;

		[field: Header("Lighting Profile")]
		[field: SerializeField] private LightingProfile m_lightingProfile;

		[Header("Orbit Settings")]
		[SerializeField] private Vector3 m_mapCenter = Vector3.zero;
		[SerializeField] private float m_orbitRadius = 500f;
		[SerializeField] private float m_orbitHeight = 300f;
		[SerializeField] private float m_orbitAngleOffset = -45f; // Adjusts which side the sun rises/sets from

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
				return time < sunrise
					? Mathf.InverseLerp(sunset, sunrise + 1, time + 1) * 0.5f + 0.5f// Night before sunrise
					: Mathf.InverseLerp(sunset, sunrise + 1, time) * 0.5f + 0.5f;// Night after sunset
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