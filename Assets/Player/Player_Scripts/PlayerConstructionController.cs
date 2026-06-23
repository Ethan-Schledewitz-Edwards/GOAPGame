using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerConstructionController : PlayerWorldControllerBase
{
	private const float c_cancleRadius = 2.0f;
	private const float c_secondsToCancle = 2.0f;

	public override string ControllerName => "Construction Mode";
	public override Sprite ControllerIcon => m_controllerIcon;
	[SerializeField] private Sprite m_controllerIcon;

	[SerializeField] private Material m_validMaterial;
	[SerializeField] private Material m_invalidMaterial;

	private bool m_isCancleHeld = false;

	private bool m_isPlacementValid;
	private BlueprintData m_blueprintData;
	private Quaternion m_placementRotation;

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
		m_blueprintData = structureData;
		m_placementRotation = Quaternion.identity;
		UpdateVisuals(false);
	}

	public override void OnControllerEnabled() 
	{
		enabled = true;
		RefreshCursor(out _);
	}

	public override void OnControllerDisabled()
	{
		enabled = false;
		ClearBlueprintData();
	}

	public override void PrimaryFire(InputAction.CallbackContext context)
	{
		if (m_isPlacementValid)
		{
			TryPlaceBlueprint(m_mouseWorldPosition, m_placementRotation);
		}
		else
		{
			Debug.Log("Placement blocked by obstacle or interaction object.");
		}
	}

	public override void SecondaryFire(InputAction.CallbackContext context) 
	{
		m_isCancleHeld = context.ReadValueAsButton();
	}

	public override void Cycle(int cycleDirection) 
	{
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

		ConstructionManager.Instance.CreateStructureBlueprint(nearestSettlementID, 
			m_blueprintData, 
			position,
			rotation);

		ClearBlueprintData();
	}

	private void ClearBlueprintData()
	{
		m_blueprintData = null;
		m_isPlacementValid = false;
		m_placementRotation = Quaternion.identity;
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
				Collider[] hitColliders = Physics.OverlapSphere(hitData.point, c_cancleRadius, m_interactionLayer);
				if (hitColliders.Length != 0)
				{
					foreach (Collider i in hitColliders)
					{
						if(i.TryGetComponent(out BlueprintIO blueprint))
						{
							ConstructionManager.Instance?.CancleBlueprint(blueprint);
						}
					}
				}
			}
		}
	}

	private void UpdateVisuals(bool isValid)
	{
		Material[] materials = { isValid ? m_validMaterial : m_invalidMaterial };

		if (m_blueprintData == null)
			return;

		Bounds bounds = m_blueprintData.BlueprintMesh.bounds;
		Vector3 localOffset = new Vector3(0, bounds.extents.y, 0);
		m_controllerManager.CursorVisualizer?.SetVisuals(m_blueprintData.BlueprintMesh, 
			materials, 
			localOffset, 
			m_placementRotation
			);
	}
}
