using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerHealthComponent), typeof(PlayerWorldControllerManager))]
public class Player : Entity
{
	[field: SerializeField] public CameraRig PlayerCamera { get; private set; }
	[field: SerializeField] public Transform PlayerMesh { get; private set; }
	public PlayerController PlayerController { get; private set; }
	public PlayerHealthComponent PlayerHealthComponent { get; private set; }
	public PlayerWorldControllerManager PlayerWorldControllerManager { get; private set; }

	protected void Awake()
	{
		PlayerController = GetComponent<PlayerController>();
		PlayerHealthComponent = GetComponent<PlayerHealthComponent>();
		PlayerWorldControllerManager = GetComponent<PlayerWorldControllerManager>();
	}

	protected override void UpdatePosition()
	{
		Vector3 prevPos = m_position;
		base.UpdatePosition();

		if (prevPos != m_position)
		{
			ActorManager.Instance.SetPlayerPosition(m_position);
		}
	}
}
