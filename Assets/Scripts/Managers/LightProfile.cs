using UnityEngine;

[CreateAssetMenu(fileName = "Lighting Profile", menuName = "Lighting Profile", order = 1)]
public class LightingProfile : ScriptableObject
{
	[field: Header("Sunrise Settings")]
	[field: SerializeField, Range(1, 24)] public int SunriseHr { get; private set; }
	[field: SerializeField, Range(1, 24)] public int SunsetHr { get; private set; }

	[field: Header("Lighting Settings")]
	[field: SerializeField] public Gradient LightingColorOverDay { get; private set; }
	[field: SerializeField] public Gradient AmbientColorOverDay { get; private set; }
	[field: SerializeField] public Gradient CloudColourOverDay { get; private set; }
	public AnimationCurve LightFadeOverDay { get; private set; } = new AnimationCurve(new Keyframe[]{
		new(0, 0),
		new(0.1f, 1),
		new(0.4f, 1),
		new(0.5f, 0),
		new(0.6f, 1),
		new(0.9f, 1),
		new(1, 0),
	});

	[field: Header("Fog Settings")]
	[field: SerializeField] public Gradient FogColorOverDay { get; private set; }
	[field: SerializeField] public AnimationCurve FogStartOverDay { get; private set; }
	[field: SerializeField] public AnimationCurve FogEndOverDay { get; private set; }
}
