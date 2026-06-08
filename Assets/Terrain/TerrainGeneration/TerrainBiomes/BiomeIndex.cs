using UnityEngine;

[CreateAssetMenu(fileName = "BiomeIndex", menuName = "Biomes/BiomeIndex")]
public class BiomeIndex : ScriptableObject
{
	[field: SerializeField] public BiomeWeighting[] Biomes { get; private set; }
	[field: SerializeField] public float TempuratureMapScale { get; private set; } = 0.002f;
	[field: SerializeField] public float HumidityMapScale { get; private set; } = 0.002f;

	[System.Serializable]
	public struct BiomeWeighting
	{
		[field: SerializeField] public TerrainBiomeData TerrainBiome { get; private set; }

		[Header("Environmental Targets")]
		[Range(0, 1)] public float TargetTemperature;
		[Range(0, 1)] public float TargetHumidity;
	}
}
