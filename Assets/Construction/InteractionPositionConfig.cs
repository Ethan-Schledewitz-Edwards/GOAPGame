using UnityEngine;

namespace Construction
{
	[System.Serializable]
	public struct InteractionPositionConfig
	{
		[field: SerializeField] public Vector3 LocalOffset { get; private set; }
		
		[Header("Settings")]
		[field: SerializeField] public int MaxInteractors { get; private set; }
		[field: SerializeField] public bool UseFormationRadius { get; private set; }
		[field: SerializeField] public float FormationRadius { get; private set; }

		[field: SerializeField, Tooltip("If false, this position does not require pre-allocation or locking, allowing multiple actors (like doors or storage access points) to share it.")]
		public bool RequiresReservation { get; private set; }
		
		[field: SerializeField, Tooltip("Dictates how far from the center of the InteractionPosition the actor can be before they begin interacting.")]
		public float InteractionDistance { get; private set; }
	}
}
