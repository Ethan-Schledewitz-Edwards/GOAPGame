using UnityEngine;

public class LightManager : MonoBehaviour
{
	[field: Header("Lighting Profile")]
	[field: SerializeField] private LightingProfile m_lightingProfile;

	// System
	private GameManager m_gameManager;

	private void Start()
	{
		m_gameManager = GameManager.Instance;
	}

	protected void Update()
	{
		if (m_lightingProfile != null)
		{
			float sunriseHr = (m_lightingProfile.SunriseHr / 24f);
			float sunsetHr = (m_lightingProfile.SunsetHr / 24f);

			float adjustedTime = NormalizeTime(m_gameManager.GetTimeOfDayFract(), sunriseHr, sunsetHr);
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
		float lightAngle = timeOfDayFract <= 0.5f
			? Mathf.Lerp(0f, 180f, timeOfDayFract * 2)// Daytime (0 to 0.5)
			: Mathf.Lerp(180f, 360f, (timeOfDayFract - 0.5f) * 2);// Nighttime (0.5 to 1)

		Light sun = RenderSettings.sun;

		if (m_lightingProfile != null)
		{
			// Fog
			RenderSettings.fogColor = m_lightingProfile.FogColorOverDay.Evaluate(timeOfDayFract);
			RenderSettings.fogStartDistance = m_lightingProfile.FogStartOverDay.Evaluate(timeOfDayFract);
			RenderSettings.fogEndDistance = m_lightingProfile.FogEndOverDay.Evaluate(timeOfDayFract);

			/*
			// Cloud colours
			foreach (MeshRenderer i in cloudLayers)
			{
				i.material.SetColor("_CloudColor", Profile.CloudColourOverDay.Evaluate(timeOfDay));
			}
			*/

			// Ambient colour
			Color ambientColor = m_lightingProfile.AmbientColorOverDay.Evaluate(timeOfDayFract);
			RenderSettings.subtractiveShadowColor = ambientColor;

			// Sun colour
			Color lightColor = m_lightingProfile.LightingColorOverDay.Evaluate(timeOfDayFract);
			float colorStrength = m_lightingProfile.LightFadeOverDay.Evaluate(timeOfDayFract);
			sun.color = Color.Lerp(ambientColor, lightColor, colorStrength);
		}

		bool isDay = timeOfDayFract <= 0.5f;
		sun.transform.rotation = Quaternion.Euler(isDay ? lightAngle : lightAngle + 180f, 45f, 0f);// Flip the light at night
	}
}
