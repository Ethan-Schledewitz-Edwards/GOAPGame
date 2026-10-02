using UnityEngine;

[System.Serializable]
public class InteractionPositionSettings
{
	// NOTE: This should eventually be removed because the blueprint should really be a prefab.
	// Who wants to type in offsets manually anyways?
	public Vector3 LocalOffset = Vector3.zero;

	public int MaxInteractors = 1;
	public bool UseFormationRadius = true;
	public float FormationRadius = 1.5f;

	[Tooltip("If false, this position does not require pre-allocation or locking, allowing multiple actors (like doors or storage access points) to share it.")]
	public bool RequiresReservation = true;

	[Tooltip("Dictates how far from the center of the InteractionPosition the actor can be before they begin interacting.")]
	public float InteractionDistance = 0.3f;
}
