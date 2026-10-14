using BehaviourTrees;
using Factions.Core;
using Interaction.Core;
using ObjectTags;
using Settlements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Actor.InteractableStructures
{
	[RequireComponent(typeof(UITrackedProgressComponent))]
	public class Duplicatinator : ActorInteractableBase, IStructure
	{
		[Header("Settings")]
		[SerializeField] private StructureTag m_duplicatinatorTag;
		[SerializeField] private Transform m_noddSpawnPoint;
		[SerializeField] private Transform m_noddPrefab;

		protected override int m_minActorsToFunction => 0; // Needs a carryable not guys

		// IInteractable properties
		protected override string m_interactPrompt => string.Empty;

		// IStructure properties
		public StructureTag StructureTypeTag => m_duplicatinatorTag;
		public int SettlementID => m_settlementID;
		public int SettlementStructureID => m_settlementStructureID;
		public GameObject Object => gameObject;

		// Components
		private UITrackedProgressComponent m_progressComponent;

		// System
		private int m_settlementID;
		private int m_settlementStructureID;

		private Coroutine m_duplicationRoutine;
		private bool m_isDuplicatingNodds;
		private float m_duplicationCountdown;
		/*Add a variable to specify which type is produced*/

		protected override void Awake()
		{
			base.Awake();

			m_progressComponent = GetComponent<UITrackedProgressComponent>();
		}

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

		private void OnDestroy()
		{
			StopCoroutine(m_duplicationRoutine);
		}

		public void SetSettlement(int settlementID, int settlementStructureID)
		{
			m_settlementID = settlementID;
			m_settlementStructureID = settlementStructureID;
		}

		public override bool TryReserveClosestPosition(IInteractor interactor, 
			Vector3 actorPosition, 
			out InteractionPoint reservedPoint)
		{
			reservedPoint = null;

			if (m_isDuplicatingNodds)
				return false;

			return base.TryReserveClosestPosition(interactor, actorPosition, out reservedPoint);
		}

		public override bool TryInteract(IInteractor interactor, Vector3 actorPosition, InteractionPoint assignedPosition)
		{
			if(m_isDuplicatingNodds) 
				return false;

			// Kick actors off of the carryable
			interactor.InteractionCanceled();

			Destroy(interactor.Transform.gameObject);

			m_duplicationRoutine = StartCoroutine(BeginNoddDuplication(4, 5f));

			return true;
		}

		private IEnumerator BeginNoddDuplication(int noddsProduced, float duration /*Add a property to specify which type to produce*/)
		{
			m_isDuplicatingNodds = true;
			m_duplicationCountdown = duration;

			while (m_duplicationCountdown > 0)
			{
				m_duplicationCountdown -= Time.deltaTime;
				m_progressComponent.UpdateProgressValue(m_duplicationCountdown, duration); // Show timer counting down
				yield return null;
			}

			m_duplicationCountdown = 0f;
			m_isDuplicatingNodds = false;
			m_progressComponent.UpdateProgressValue(0, duration); // Hide timer

			int columns = Mathf.CeilToInt(Mathf.Sqrt(noddsProduced));
			float spacing = 0.5f;

			for (int i = 0; i < noddsProduced; i++)
			{
				int x = i % columns;
				int y = i / columns;

				Vector2 xzOffset = new Vector2(x * spacing, y * spacing);

				SpawnNodd(xzOffset);
			}
		}

		private void SpawnNodd(Vector2 offset)
		{
			if(m_noddSpawnPoint == null ||
				m_noddPrefab == null)
				return;

			Vector3 spawnPosition = m_noddSpawnPoint.position + 
				new Vector3(offset.x, 0, offset.y);

			Instantiate(m_noddPrefab,
				spawnPosition, 
				m_noddSpawnPoint.rotation, null);
		}

		public override BehaviourTree GetBehaviourTree() { return null; }
	}
}
