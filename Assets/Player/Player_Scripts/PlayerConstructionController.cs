using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Construction;
using Factions.Core;
using Interaction.ActorInteractables.Structures;

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
		private PlayerCursorVisualizer m_cursorVisualizer;

		// System
		private bool m_isCancleHeld = false;
		private Vector3 m_cursorWorldPosition;
		private bool m_isPlacementValid;
		private Quaternion m_placementRotation;

		private BlueprintData m_blueprintDataToPlace;

		private IBlueprintObject m_multiPointBlueprint;
		private int m_pointsPlaced;
		private List<Vector3> m_currentPoints = new List<Vector3>();

		HashSet<Collider> m_collidersBeingCanceled;

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();

			if (m_blockingLayer == 0) 
				m_blockingLayer = LayerMask.GetMask("Player", "Actor");

			if (m_interactionLayer == 0) 
				m_interactionLayer = LayerMask.GetMask("Interaction");

			m_collidersBeingCanceled = new HashSet<Collider>();

			m_followerController = GetComponent<PlayerFollowerController>();
			m_cursorVisualizer = m_worldControllerManager.CursorVisualizer;
		}

		private void Start()
		{
			if (ConstructionManager.Instance != null)
				ConstructionManager.Instance.NewDevelopmentAttempted += SetBlueprintToPlace;

			RefreshPlacementValidityVisuals(false);
		}

		private void OnDestroy()
		{
			if (ConstructionManager.Instance != null)
				ConstructionManager.Instance.NewDevelopmentAttempted -= SetBlueprintToPlace;
		}
		#endregion

		#region Input

		protected override void OnPrimaryFireInput(InputAction.CallbackContext context)
		{
			if (m_blueprintDataToPlace == null || !context.ReadValueAsButton())
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
			if (m_blueprintDataToPlace != null)
			{
				ClearBlueprintToPlace();
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
			RefreshPlacementValidityVisuals(m_isPlacementValid);
		}

		#endregion

		public override void RefreshCursor(Vector3 worldPosition)
		{
			if (InputManager.ControlMode == InputManager.ControlType.Player)
			{
				m_cursorWorldPosition = worldPosition;

				Vector3 placementPosition = worldPosition;
				LayerMask combinedCheckMask = m_blockingLayer | m_interactionLayer;
				if (m_blueprintDataToPlace != null)
				{
					Vector3 extents = m_blueprintDataToPlace.PlacementMesh.bounds.extents;
					float baseRadius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
					float clearance = baseRadius + m_blueprintDataToPlace.PlacementClearenceRadius;

					bool isBlocked = Physics.CheckSphere(
						placementPosition,
						clearance,
						combinedCheckMask
					);

					if (isBlocked != !m_isPlacementValid)
					{
						m_isPlacementValid = !isBlocked;
					}

					UpdateRealTimeMultiPointVisuals(m_isPlacementValid);
					return;
				}

				if (m_isCancleHeld)
				{
					float selectionRadius = m_worldControllerManager.CursorVisualizer.SelectionRadius;
					HashSet<Collider> hitColliders = Physics.OverlapSphere(worldPosition, selectionRadius, m_interactionLayer).ToHashSet();

					foreach (Collider i in hitColliders)
					{
						if (i != null && !m_collidersBeingCanceled.Contains(i) &&
							i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
							!blueprintCancelation.IsBeingCanceled)
						{
							blueprintCancelation.BeginCancelation();
						}
					}

					if (m_collidersBeingCanceled.Count > 0)
					{
						foreach (Collider i in m_collidersBeingCanceled)
						{
							if (i != null && !hitColliders.Contains(i) &&
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
					if (m_collidersBeingCanceled.Count > 0)
					{
						foreach (Collider i in m_collidersBeingCanceled)
						{
							if (i != null && i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
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

		private void SetBlueprintToPlace(BlueprintData structureData)
		{
			if (m_followerController != null)
				m_followerController.enabled = false;

			m_pointsPlaced = 0;
			m_multiPointBlueprint = null;
			m_blueprintDataToPlace = structureData;
			m_placementRotation = Quaternion.identity;
			m_currentPoints.Clear();

			PlayerCursorVisualizer cursorVisualizer = m_worldControllerManager?.CursorVisualizer;
			cursorVisualizer?.ClearBlueprintPlacementVisuals();

			RefreshPlacementValidityVisuals(false);
		}

		private void ClearBlueprintToPlace()
		{
			m_blueprintDataToPlace = null;
			m_isPlacementValid = false;
			m_placementRotation = Quaternion.identity;
			m_multiPointBlueprint = null;
			m_pointsPlaced = 0;
			m_currentPoints.Clear();

			PlayerCursorVisualizer cursorVisualizer = m_worldControllerManager?.CursorVisualizer;
			cursorVisualizer?.ClearBlueprintPlacementVisuals();

			RefreshPlacementValidityVisuals(false);

			if (m_followerController != null)
				m_followerController.enabled = true;
		}

		private void CompleteBlueprintPlacement()
		{
			PlayerCursorVisualizer cursorVisualizer = m_worldControllerManager?.CursorVisualizer;
			cursorVisualizer?.ClearBlueprintPlacementVisuals();

			// Generate permanent meshes directly on the placed blueprint using blueprint material (blue)
			if (m_blueprintDataToPlace != null)
			{
				Transform blueprintTransform = GetBlueprintTransform(m_multiPointBlueprint);
				if (blueprintTransform != null)
				{
					cursorVisualizer?.GeneratePermanentSegments(blueprintTransform, m_blueprintDataToPlace, m_currentPoints);
				}
			}

			m_blueprintDataToPlace = null;
			m_isPlacementValid = false;
			m_placementRotation = Quaternion.identity;
			m_multiPointBlueprint = null;
			m_pointsPlaced = 0;
			m_currentPoints.Clear();

			cursorVisualizer?.EnableCursor();

			if (m_followerController != null)
				m_followerController.enabled = true;
		}

		private void TryPlaceBlueprint(Vector3 position, Quaternion rotation)
		{
			if (m_blueprintDataToPlace == null)
				return;

			int nearestSettlementID =
				SettlementManager.GetClosestSettlementID(transform.position, EFaction.FACTION_PLAYER);

			if (nearestSettlementID == -1)
				SettlementManager.Instance.CreatePlayerSettlement(position, out nearestSettlementID);

			if (m_blueprintDataToPlace.IsMultiPointBlueprint)
			{
				if (m_pointsPlaced == 0)
				{
					ConstructionManager.Instance.CreateBlueprint
					(
						nearestSettlementID,
						m_blueprintDataToPlace,
						position,
						rotation,
						out m_multiPointBlueprint
					);
					m_currentPoints.Add(position);
				}
				else if (m_multiPointBlueprint != null)
				{
					Transform bpTransform = GetBlueprintTransform(m_multiPointBlueprint);
					if (bpTransform != null && m_currentPoints.Count > 0)
					{
						Vector3 dir = position - m_currentPoints[0];
						if (dir.sqrMagnitude > 0.001f)
						{
							bpTransform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
						}
					}

					m_multiPointBlueprint.HandlePointAdded(position);
					m_currentPoints.Add(position);
				}

				m_pointsPlaced++;

				if (m_pointsPlaced >= m_blueprintDataToPlace.MaxPoints)
					CompleteBlueprintPlacement();
			}
			else
			{
				ConstructionManager.Instance.CreateBlueprint
				(
					nearestSettlementID,
					m_blueprintDataToPlace,
					position,
					rotation,
					out _
				);

				ClearBlueprintToPlace();
			}
		}

		private Transform GetBlueprintTransform(IBlueprintObject blueprintObj)
		{
			if (blueprintObj == null) return null;
			if (blueprintObj is Component comp) return comp.transform;
			if (blueprintObj is MonoBehaviour mb) return mb.transform;
			var prop = blueprintObj.GetType().GetProperty("transform");
			if (prop != null && prop.PropertyType == typeof(Transform))
				return prop.GetValue(blueprintObj) as Transform;
			return null;
		}

		private void RefreshPlacementValidityVisuals(bool isPlacementValid)
		{
			if (m_blueprintDataToPlace == null)
			{
				m_cursorVisualizer.ClearBlueprintPlacementVisuals();
				m_cursorVisualizer.EnableCursor();
				return;
			}

			Material[] materials = { isPlacementValid ? m_validMaterial : m_invalidMaterial };

			if (m_blueprintDataToPlace.IsMultiPointBlueprint && m_pointsPlaced > 0)
			{
				UpdateRealTimeMultiPointVisuals(isPlacementValid);
			}
			else
			{
				m_cursorVisualizer.SetBlueprintPlacementVisuals(
					m_blueprintDataToPlace,
					materials,
					m_placementRotation
				);
			}
		}

		private void UpdateRealTimeMultiPointVisuals(bool isPlacementValid)
		{
			if (m_cursorVisualizer == null) 
				return;

			Material[] materials = { isPlacementValid ? m_validMaterial : m_invalidMaterial };

			if (m_blueprintDataToPlace.IsMultiPointBlueprint && m_pointsPlaced > 0)
			{
				// Rotate the original placed blueprint object in real-time to face the cursor
				Transform bpTransform = GetBlueprintTransform(m_multiPointBlueprint);
				if (bpTransform != null && m_currentPoints.Count > 0)
				{
					Vector3 dirToCursor = m_cursorWorldPosition - m_currentPoints[0];
					if (dirToCursor.sqrMagnitude > 0.001f)
					{
						bpTransform.rotation = Quaternion.LookRotation(dirToCursor.normalized, Vector3.up);
					}
				}

				// Pass confirmed points and current live cursor position separately
				m_cursorVisualizer.SetMultiPointBlueprintPreviewVisuals(
					m_blueprintDataToPlace,
					m_currentPoints,
					m_cursorWorldPosition,
					materials
				);
			}
			else
			{
				cursorVisualizer?.SetBlueprintPlacementVisuals(
					m_blueprintDataToPlace,
					materials,
					m_placementRotation
				);
			}
		}
	}
}