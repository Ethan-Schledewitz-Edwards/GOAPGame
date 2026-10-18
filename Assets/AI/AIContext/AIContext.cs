using Interaction.Core;
using ObjectTags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI.AIContext 
{
	[System.Serializable]
	public struct AIContext
	{
		// Behavior Tree and Execution 
		private HashSet<string> m_activeNodeIDs;

		public HashSet<string> ActiveNodeIDs
		{
			get
			{
				if (m_activeNodeIDs == null)
					m_activeNodeIDs = new HashSet<string>();

				return m_activeNodeIDs;
			}
		}


		public float Timeout;
		public float Cooldown;
		private Dictionary<string, int> m_sequenceIndices;
		public Dictionary<string, int> SequenceIndices
		{
			get
			{
				if (m_sequenceIndices == null)
					m_sequenceIndices = new Dictionary<string, int>();
				return m_sequenceIndices;
			}
		}
		public Transform ExecutorTransform;
		public int ExecutorFaction;

		// Targeting and Navigation
		public Transform TargetTransform;
		public Vector3 TargetDestination;
		public float JobSearchDistance;
		public LayerMask InteractionLayer;
		public InteractionPoint ReservedInteractionPoint;

		// Interactions
		public int AttackDamage;
		public bool IsCarryingObject;

		// Inventory
		private string m_heldItemID;
		public string HeldItemID
		{
			get => m_heldItemID ?? string.Empty;
			set => m_heldItemID = value;
		}

		private string m_itemToFindID;
		public string ItemToFindID
		{
			get => m_itemToFindID ?? string.Empty;
			set => m_itemToFindID = value;
		}
		public bool DepositInteractionStarted;

		// Structures
		public int StructureSettlementID;
		public int StructureID;

		// Filters
		private string[] m_itemTagFilters;
		public string[] ItemTagFilters
		{
			get => m_itemTagFilters ?? Array.Empty<string>();
			set => m_itemTagFilters = value;
		}

		private ResourceTypeTag[] m_cachedResolvedItemTags;
		public ResourceTypeTag[] CachedResolvedItemTags
		{
			get => m_cachedResolvedItemTags ?? Array.Empty<ResourceTypeTag>();
			set => m_cachedResolvedItemTags = value;
		}

		// Cleanup
		public System.Action ReservationCleanup;

		public void ClearAllData()
		{
			this = new AIContext();
		}
	}
}