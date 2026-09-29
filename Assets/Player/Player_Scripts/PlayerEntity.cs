using System.Collections;
using UnityEngine;

namespace Player.Core
{
	[RequireComponent(typeof(PlayerController), typeof(PlayerHealthComponent))]
	public class PlayerEntity : MonoBehaviour
	{
		[field: SerializeField] public PlayerCamera PlayerCamera { get; private set; }
		[field: SerializeField] public Transform PlayerMesh { get; private set; }
		public PlayerController PlayerController { get; private set; }
		public PlayerHealthComponent PlayerHealthComponent { get; private set; }
	}
}