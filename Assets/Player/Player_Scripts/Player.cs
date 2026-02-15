using UnityEngine;

public class Player : HealthComponent
{
	[field: SerializeField] public CameraRig PlayerCamera { get; private set; }

	[field: SerializeField] public Transform PlayerMesh { get; private set; }
}
