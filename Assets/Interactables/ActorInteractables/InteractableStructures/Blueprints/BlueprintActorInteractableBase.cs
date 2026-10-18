using BehaviourTrees;
using Construction;
using AssetIndex.Core;
using NoddTracking.Core;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;
using Interaction.Core;

namespace Interaction.ActorInteractables.Structures
{
	[RequireComponent(typeof(NoddRequestComponent))]
	public abstract class BlueprintActorInteractableBase : ActorInteractableBase, IStructure
	{
		[Header("Structure Settings")]
		[SerializeField] private StructureTag m_structureTypeTag;

		protected NoddRequestComponent m_noddRequestComponent;

		protected int m_settlementID;
		protected int m_settlementStructureID;

		// IInteractable properties
		protected override string m_interactPrompt => string.Empty;

		// IStructure properties
		public StructureTag StructureTypeTag => m_structureTypeTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		// IItemFiltered properties
		[SerializeField] protected ResourceTypeTag[] m_tagFilter;
		public ResourceTypeTag[] ItemTagFilter => m_tagFilter;

		protected override void Awake()
		{
			base.Awake();

			m_noddRequestComponent = GetComponent<NoddRequestComponent>();
			m_noddRequestComponent.ResourcesAchieved += HandleBlueprintCompleted;
		}

		private void OnDestroy()
		{
			if (m_noddRequestComponent != null)
				m_noddRequestComponent.ResourcesAchieved -= HandleBlueprintCompleted;
		}

		public override bool TryInteract(
			IInteractor interactor,
			Vector3 actorPosition,
			InteractionPoint reservedPosition)
		{
			// Get the behaviour tree through base interaction
			if (!base.TryInteract(interactor, actorPosition, reservedPosition))
				return false;

			if (m_noddRequestComponent == null)
			{
				base.StopInteract(interactor, reservedPosition);
				return false;
			}

			// Tell the actor which items to find (Its behaviour tree will use the context to find the item)
			return false;
		}

		public void SetSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public abstract void HandleBlueprintCompleted();

		public override BehaviourTree GetBehaviourTree() => null;
	}
}
