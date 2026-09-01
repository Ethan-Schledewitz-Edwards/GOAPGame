using Interaction.InteractableStructures.Blueprints;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Construction;

namespace Player.Core
{
	public class PlayerConstructionController : PlayerWorldControllerBase
	{
		[Header("Settings")]
		private LayerMask m_blockingLayer;
		private LayerMask m_interactionLayer;

		[Header("Visuals")]
		[SerializeField] private Material m_validMaterial;
		[SerializeField] private Material m_invalidMaterial;

		// Components
		private PlayerFollowerController m_followerController;

		// System
		private bool m_isCancleHeld = false;
		private bool m_isPlacementValid;

		private BlueprintData m_blueprintData;
		private Quaternion m_placementRotation;

		private Vector3 m_cursorWorldPosition;
		HashSet<Collider> m_collidersBeingCanceled;

		protected override void Awake()
		{
			base.Awake();

			if (m_blockingLayer == 0) m_blockingLayer = LayerMask.GetMask("Player", "Actor");
			if (m_interactionLayer == 0) m_interactionLayer = LayerMask.GetMask("Interaction");
			m_collidersBeingCanceled = new HashSet<Collider>();

			m_followerController = GetComponent<PlayerFollowerController>();
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

		protected override void OnPrimaryFireInput(InputAction.CallbackContext context)
		{
			if (m_blueprintData == null || !context.ReadValueAsButton())
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

		public override void RefreshCursor(Vector3 worldPosition)
		{
			if (InputManager.ControlMode == InputManager.ControlType.Player)
			{
				m_cursorWorldPosition = worldPosition;

				// Use the point already calculated by the base class
				Vector3 placementPosition = worldPosition;
				LayerMask combinedCheckMask = m_blockingLayer | m_interactionLayer;
				if (m_blueprintData != null)
				{
					Vector3 extents = m_blueprintData.BlueprintMesh.bounds.extents;
					float baseRadius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
					float clearance = baseRadius + m_blueprintData.PlacementClearenceRadius;

					bool isBlocked = Physics.CheckSphere(
						placementPosition,
						clearance,
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
					float selectionRadius = m_worldControllerManager.CursorVisualizer.SelectionRadius;

					HashSet<Collider> hitColliders = Physics.OverlapSphere(worldPosition, selectionRadius, m_interactionLayer).ToHashSet();
					if (hitColliders.Count > 0)
					{
						// Check new colliders
						foreach (Collider i in hitColliders)
						{
							if (i != null &&
								!m_collidersBeingCanceled.Contains(i) &&
								i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
								!blueprintCancelation.IsBeingCanceled)
							{
								blueprintCancelation.BeginCancelation();
							}
						}
					}

					// Compare old colliders & cancel those that were previously tracked but werent in the latest overlap check
					if (m_collidersBeingCanceled.Count > 0)
					{
						foreach (Collider i in m_collidersBeingCanceled)
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

					m_collidersBeingCanceled = hitColliders;
				}
				else
				{
					// Stop cancellation on everything currently tracked
					if (m_collidersBeingCanceled.Count > 0)
					{
						foreach (Collider i in m_collidersBeingCanceled)
						{
							if (i != null &&
								i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
								blueprintCancelation.IsBeingCanceled)
							{
								blueprintCancelation.StopCancelation();
							}
						}
						m_collidersBeingCanceled.Clear();
					}
				}
			}
		}

		private void SetBlueprint(BlueprintData structureData)
		{
			if (m_followerController != null)
				m_followerController.enabled = false;

			m_blueprintData = structureData;
			m_placementRotation = Quaternion.identity;
			UpdateVisuals(false);
		}

		private void ClearBlueprintData()
		{
			m_blueprintData = null;
			m_isPlacementValid = false;
			m_placementRotation = Quaternion.identity;
			UpdateVisuals();

			if (m_followerController != null)
				m_followerController.enabled = true;
		}

		private void TryPlaceBlueprint(Vector3 position, Quaternion rotation)
		{
			if (m_blueprintData == null)
				return;

			int nearestSettlementID = SettlementManager.GetClosestSettlementID(transform.position, true, true);

			// Create a new player settlement if none were found
			if (nearestSettlementID == -1)
				SettlementManager.Instance.CreatePlayerSettlement(position, out nearestSettlementID);

			ConstructionManager.Instance.CreateBlueprint
			(
				nearestSettlementID,
				m_blueprintData.BlueprintDataID,
				position,
				rotation
			);

			ClearBlueprintData();
		}

		private void UpdateVisuals(bool isValid = false)
		{
			PlayerCursorVisualizer cursorVisualizer = m_worldControllerManager.CursorVisualizer;

			if (m_blueprintData == null)
			{
				cursorVisualizer?.DisableBlueprint();
				cursorVisualizer?.EnableCursor();
				return;
			}

			Material[] materials = { isValid ? m_validMaterial : m_invalidMaterial };

			Bounds bounds = m_blueprintData.BlueprintMesh.bounds;
			Vector3 localOffset = new Vector3(0, bounds.extents.y, 0);
			cursorVisualizer?.SetBlueprint(m_blueprintData,
				materials,
				m_placementRotation
				);
		}
	}
}