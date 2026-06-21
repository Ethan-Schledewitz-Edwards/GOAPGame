using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerConstructionController : PlayerWorldControllerBase
{
	public override string ControllerName => "Construction Mode";
	public override Sprite ControllerIcon => m_controllerIcon;
	[SerializeField] private Sprite m_controllerIcon;

	[SerializeField] private Material m_validMaterial;
	[SerializeField] private Material m_invalidMaterial;

	public event Action<BlueprintData, Vector3> PlacedStructure;

	private bool m_isPlacementValid;
	private BlueprintData m_blueprintStructureData;

	private LayerMask m_blockingLayer;
	private LayerMask m_interactionLayer;

	protected override void Awake()
	{
		base.Awake();
		if (m_blockingLayer == 0) m_blockingLayer = LayerMask.GetMask("Player", "Actor");
		if (m_interactionLayer == 0) m_interactionLayer = LayerMask.GetMask("Interaction");
	}

	private void Start()
	{
		if (ConstructionManager.Instance != null)
			ConstructionManager.Instance.NewDevelopmentAttempted += SetBlueprint;

		enabled = false;
	}

	private void OnDestroy()
	{
		if (ConstructionManager.Instance != null)
			ConstructionManager.Instance.NewDevelopmentAttempted -= SetBlueprint;
	}

	private void SetBlueprint(BlueprintData structureData)
	{
		m_blueprintStructureData = structureData;
		UpdateVisuals(true);
	}

	public override void OnControllerEnabled() 
	{
		enabled = true;
		RefreshCursor(out _);
	}

	public override void OnControllerDisabled()
	{
		enabled = false;
		CancleBlueprintPlacement();
	}

	public override void PrimaryFire(InputAction.CallbackContext context)
	{
		if (m_isPlacementValid)
		{
			TryPlaceBlueprint(m_mouseWorldPosition, Quaternion.identity);
		}
		else
		{
			Debug.Log("Placement blocked by obstacle or interaction object.");
		}
	}

	public override void SecondaryFire(InputAction.CallbackContext context) { }

	public override void Cycle(int scrollDir) { }

	private void TryPlaceBlueprint(Vector3 position, Quaternion rotation)
	{
		if (m_blueprintStructureData == null)
			return;

		int nearestSettlementID = SettlementManager.GetClosestSettlementID(transform.position, true, true);

		// Create a new player settlement if none were found
		if (nearestSettlementID == -1)
			SettlementManager.Instance.CreatePlayerSettlement(position, out nearestSettlementID);

		ConstructionManager.Instance.CreateStructureBlueprint(nearestSettlementID, 
			m_blueprintStructureData, 
			position,
			rotation);

		CancleBlueprintPlacement();
	}

	private void CancleBlueprintPlacement()
	{
		m_blueprintStructureData = null;
		m_controllerManager.CursorVisualizer?.ReturnToDefaultVisuals();
	}

	protected override void RefreshCursor(out RaycastHit hitData)
	{
		base.RefreshCursor(out hitData);

		if (hitData.collider != null)
		{
			// Use the point already calculated by the base class
			Vector3 placementPosition = hitData.point;
			LayerMask combinedCheckMask = m_blockingLayer | m_interactionLayer;
			if (m_blueprintStructureData != null)
			{
				bool isBlocked = Physics.CheckSphere(
					placementPosition,
					m_blueprintStructureData.PlacementClearenceRadius,
					combinedCheckMask
				);

				if (isBlocked != !m_isPlacementValid)
				{
					m_isPlacementValid = !isBlocked;
					UpdateVisuals(m_isPlacementValid);
				}
			}
		}
	}

	private void UpdateVisuals(bool isValid)
	{
		Material[] materials = { isValid ? m_validMaterial : m_invalidMaterial };

		Bounds bounds = m_blueprintStructureData.BlueprintMesh.bounds;
		Vector3 localOffset = new Vector3(0, bounds.extents.y, 0);

		m_controllerManager.CursorVisualizer?.SetVisuals(m_blueprintStructureData.BlueprintMesh, materials, localOffset);
	}
}
