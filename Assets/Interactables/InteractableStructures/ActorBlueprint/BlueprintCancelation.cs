using System;
using System.Collections;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
    public class BlueprintCancelation : MonoBehaviour
    {
		private const float c_cancelationDuration = 3.0f;
		private const float c_maxShakeMagnitude = 0.1f;
		private const float c_maxShakeFrequency = 15.0f;

		public event Action CanceledBlueprint;

		public bool IsBeingCanceled { get; private set; }
		private Coroutine m_cancelationCoroutine;
		private float m_shakeStrength;

		public void BeginCancelation()
		{
			IsBeingCanceled = true;

			if (m_cancelationCoroutine != null)
				StopCoroutine(m_cancelationCoroutine);

			m_cancelationCoroutine = StartCoroutine(ShakeCoroutine(c_cancelationDuration,
				c_maxShakeMagnitude,
				c_maxShakeFrequency));
		}

		public void StopCancelation()
		{
			IsBeingCanceled = false;

			if (m_cancelationCoroutine != null)
				StopCoroutine(m_cancelationCoroutine);
		}

		private IEnumerator ShakeCoroutine(float duration, float magnitude, float frequency)
		{
			Vector3 originalPosition = transform.position;
			float elapsedTime = 0f;

			// Generate a random starting point in the Perlin noise map so shakes feel unique
			float randomSeedX = UnityEngine.Random.Range(0f, 100f);
			float randomSeedY = UnityEngine.Random.Range(0f, 100f);

			while (elapsedTime < duration)
			{
				elapsedTime += Time.deltaTime;

				float progress = Mathf.Clamp01(elapsedTime / duration);
				float currentMagnitude = magnitude * progress;

				float noiseX = Mathf.PerlinNoise(randomSeedX + elapsedTime * frequency, 0f) * 2f - 1f;
				float noiseY = Mathf.PerlinNoise(0f, randomSeedY + elapsedTime * frequency) * 2f - 1f;

				transform.localPosition = new Vector3(
					originalPosition.x + (noiseX * currentMagnitude),
					originalPosition.y,
					originalPosition.z
				);

				yield return null;
			}

			// Always force reset back to the exact initial position
			transform.localPosition = originalPosition;
			m_cancelationCoroutine = null;

			CanceledBlueprint?.Invoke();
		}
	}
}
