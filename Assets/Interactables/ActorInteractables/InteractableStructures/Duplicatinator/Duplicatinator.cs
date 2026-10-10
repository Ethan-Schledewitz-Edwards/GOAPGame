using BehaviourTrees;
using Factions.Core;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Actor.InteractableStructures
{
	public class Duplicatinator : MonoBehaviour, IStructure
	{
		[Header("Settings")]
		[SerializeField] private StructureTag m_duplicatinatorTag;

		// IStructure properties
		public StructureTag StructureTypeTag => m_duplicatinatorTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

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
	}
}
