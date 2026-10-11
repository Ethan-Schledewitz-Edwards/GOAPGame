using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AIContext
{
	// Behavior Tree and Execution 
	public string CurrentNodeID;
	public float Timeout;
	public readonly Dictionary<string, int> SequenceIndices;
	public Transform ExecutorTransform;
	public int ExecutorFaction;

	// Targeting and Navigation
	public Transform TargetTransform;
	public Vector3 TargetDestination;
	public float JobSearchRange;
	public LayerMask InteractionLayer;
	public Vector3 AssignedInteractionPosition;

	// Inventory
	public int HeldItemID;
	public int ItemToFindID;

	// Structures
	public int StructureSettlementID;
	public int StructureID;

	// Filters
	public string[] ItemTagFilters;

	// Cleanup
	public System.Action ReservationCleanup;

	public void ClearAllData()
	{
		this = new AIContext();
	}
}
