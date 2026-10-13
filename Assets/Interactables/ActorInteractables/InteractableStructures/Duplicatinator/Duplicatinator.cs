using BehaviourTrees;
using Factions.Core;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Actor.InteractableStructures
{
	public class Duplicatinator : ActorInteractableBase, IStructure
	{
		[Header("Settings")]
		[SerializeField] private StructureTag m_duplicatinatorTag;

		// IStructure properties
		public StructureTag StructureTypeTag => m_duplicatinatorTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		protected override int m_minActorsToFunction => 0; // Needs a log not people

		protected override string m_interactPrompt => string.Empty;

		protected override bool m_hasTrackingDisplayInfo => false; // Will have this in the future for progress bars

		// System
		private int m_settlementID;
		private int m_settlementStructureID;

		private void Start()
		{
			Settlement playerSettlement = SettlementManager.GetClosestSettlement(transform.position, EFaction.FACTION_PLAYER);
			if (playerSettlement != null)
			{
				playerSettlement.AddStructure(this);
			}
			else
			{
				Debug.LogWarning("Tried to add an EnvironmentalBlueprint to " +
					"a settlement that does not exist", this);
			}
		}

		public void SetSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public override BehaviourTree GetBehaviourTree() { return null; }
	}
}
