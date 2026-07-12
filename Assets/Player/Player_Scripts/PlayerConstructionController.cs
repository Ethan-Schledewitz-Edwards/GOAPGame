using Interaction.InteractableStructures.Blueprints;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Construction;

public class PlayerConstructionController : PlayerWorldControllerBase
{
	[SerializeField] private Material m_validMaterial;
	[SerializeField] private Material m_invalidMaterial;

	private bool m_isCancleHeld = false;

	private bool m_isPlacementValid;
	private StructureBlueprintData m_blueprintData;
	private Quaternion m_placementRotation;

	private LayerMask m_blockingLayer;
	private LayerMask m_interactionLayer;

	HashSet<Collider> collidersBeingCanceled;

	protected override void Awake()
	{
		base.Awake();
		if (m_blockingLayer == 0) m_blockingLayer = LayerMask.GetMask("Player", "Actor");
		if (m_interactionLayer == 0) m_interactionLayer = LayerMask.GetMask("Interaction");
		collidersBeingCanceled = new HashSet<Collider>();
	}

	private void Start()
	{
		if (ConstructionManager.Instance != null)
			ConstructionManager.Instance.NewDevelopmentAttempted += SetBlueprint;

		UpdateVisuals();
	}

	private void OnDestroy()
	{
		if (ConstructionManager.Instance != null)
			ConstructionManager.Instance.NewDevelopmentAttempted -= SetBlueprint;
	}

	private void SetBlueprint(StructureBlueprintData structureData)
	{
		m_blueprintData = structureData;
		m_placementRotation = Quaternion.identity;
		UpdateVisuals(false);
	}

	protected override void OnPrimaryFireInput(InputAction.CallbackContext context)
	{
		if (m_blueprintData == null)
			return;

		if (m_isPlacementValid)
		{
			TryPlaceBlueprint(m_cursorWorldPosition, m_placementRotation);
		}
		else
		{
			Debug.Log("Placement blocked by obstacle or interaction object.");
		}
	}

	protected override void OnSecondaryFireInput(InputAction.CallbackContext context) 
	{
		if (m_blueprintData != null)
		{
			ClearBlueprintData();
			m_isCancleHeld = false;
			return;
		}

		m_isCancleHeld = context.ReadValueAsButton();
	}

	protected override void OnCycleInput(InputAction.CallbackContext context) 
	{
		int cycleDirection = Math.Sign(context.ReadValue<float>());

		Quaternion rotationDelta = Quaternion.Euler(0, 90 * cycleDirection, 0);
		m_placementRotation = m_placementRotation * rotationDelta;
		UpdateVisuals(m_isPlacementValid);
	}

	private void TryPlaceBlueprint(Vector3 position, Quaternion rotation)
	{
		if (m_blueprintData == null)
			return;

		int nearestSettlementID = SettlementManager.GetClosestSettlementID(transform.position, true, true);

		// Create a new player settlement if none were found
		if (nearestSettlementID == -1)
			SettlementManager.Instance.CreatePlayerSettlement(position, out nearestSettlementID);

		ConstructionManager.Instance.CreateStructureBlueprint
		(
			nearestSettlementID, 
			m_blueprintData.StructureBlueprintID, 
			position,
			rotation
		);

		ClearBlueprintData();
	}

	private void ClearBlueprintData()
	{
		m_blueprintData = null;
		m_isPlacementValid = false;
		m_placementRotation = Quaternion.identity;
		UpdateVisuals();
	}

	protected override void RefreshCursor()
	{
		base.RefreshCursor();

		// Use the point already calculated by the base class
		Vector3 placementPosition = m_cursorWorldPosition;
		LayerMask combinedCheckMask = m_blockingLayer | m_interactionLayer;
		if (m_blueprintData != null)
		{
			bool isBlocked = Physics.CheckSphere(
				placementPosition,
				m_blueprintData.PlacementClearenceRadius,
				combinedCheckMask
			);

			if (isBlocked != !m_isPlacementValid)
			{
				m_isPlacementValid = !isBlocked;
				UpdateVisuals(m_isPlacementValid);
			}
			return;
		}

		if (m_isCancleHeld)
		{
			float selectionRadius = m_cursorVisualizer.SelectionRadius;

			HashSet<Collider> hitColliders = Physics.OverlapSphere(m_cursorWorldPosition, selectionRadius, m_interactionLayer).ToHashSet();
			if (hitColliders.Count > 0)
			{
				// Check new colliders
				foreach (Collider i in hitColliders)
				{
					if (i != null &&
						!collidersBeingCanceled.Contains(i) &&
						i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
						!blueprintCancelation.IsBeingCanceled)
					{
						blueprintCancelation.BeginCancelation();
					}
				}
			}

			// Compare old colliders & cancel those that were previously tracked but werent in the latest overlap check
			if (collidersBeingCanceled.Count > 0)
			{
				foreach (Collider i in collidersBeingCanceled)
				{
					if (i != null &&
						!hitColliders.Contains(i) &&
						i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
						blueprintCancelation.IsBeingCanceled)
					{
						blueprintCancelation.StopCancelation();
					}
				}
			}

			collidersBeingCanceled = hitColliders;
		}
		else
		{
			// Stop cancellation on everything currently tracked
			if (collidersBeingCanceled.Count > 0)
			{
				foreach (Collider i in collidersBeingCanceled)
				{
					if (i != null &&
						i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
						blueprintCancelation.IsBeingCanceled)
					{
						blueprintCancelation.StopCancelation();
					}
				}
				collidersBeingCanceled.Clear();
			}
		}
	}

	private void UpdateVisuals(bool isValid = false)
	{
		if (m_blueprintData == null)
		{
			m_cursorVisualizer?.DisableBlueprint();
			m_cursorVisualizer?.EnableCursor();
			return;
		}

		Material[] materials = { isValid ? m_validMaterial : m_invalidMaterial };

		Bounds bounds = m_blueprintData.BlueprintMesh.bounds;
		Vector3 localOffset = new Vector3(0, bounds.extents.y, 0);
		m_cursorVisualizer?.SetBlueprint(m_blueprintData.BlueprintMesh, 
			materials, 
			localOffset, 
			m_placementRotation
			);
	}
}
