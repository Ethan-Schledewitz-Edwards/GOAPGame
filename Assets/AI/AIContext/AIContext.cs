using Interaction.Core;
using ObjectTags;
using System.Collections.Generic;
using UnityEngine;

namespace AI.AIContext 
{
	[System.Serializable]
	public struct AIContext
	{
		// Behavior Tree and Execution 
		public string CurrentNodeID;
		public float Timeout;
		public float Cooldown;
		public readonly Dictionary<string, int> SequenceIndices;
		public Transform ExecutorTransform;
		public int ExecutorFaction;

		// Targeting and Navigation
		public Transform TargetTransform;
		public Vector3 TargetDestination;
		public float StoppingDistance;
		public float JobSearchDistance;
		public LayerMask InteractionLayer;
		public InteractionPoint AssignedInteractionPosition;

		// Carrying
		public bool IsCarryingObject;

		// Inventory
		public string HeldItemID;
		public string ItemToFindID;
		public bool DepositInteractionStarted;

		// Structures
		public int StructureSettlementID;
		public int StructureID;

		// Filters
		public string[] ItemTagFilters;
		public ItemTag[] CachedResolvedItemTags;

		// Cleanup
		public System.Action ReservationCleanup;

		public void ClearAllData()
		{
			this = new AIContext();
		}
	}

}