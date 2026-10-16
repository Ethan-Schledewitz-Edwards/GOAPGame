using Construction;
using Factions.Core;
using Interaction.ActorInteractables.Structures;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Player.Core
{
	public class PlayerConstructionController : PlayerWorldControllerBase
	{
		[Header("Settings")]
		private LayerMask m_blockingLayer;
		private LayerMask m_interactionLayer;

		[Header("Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
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

		Collider[] m_newCancelColliders = new Collider[16];
		Collider[] m_collidersBeingCanceled = new Collider[16];

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();

			if (m_blockingLayer == 0) 
				m_blockingLayer = LayerMask.GetMask("Player", "Actor");

			if (m_interactionLayer == 0) 
				m_interactionLayer = LayerMask.GetMask("Interaction");

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

			Quaternion rotationDelta = Quaternion.Euler(0, 45f * cycleDirection, 0);
			m_placementRotation = m_placementRotation * rotationDelta;
			RefreshPlacementValidityVisuals(m_isPlacementValid);
		}

		#endregion

		public override void RefreshCursor(Vector3 worldPosition)
		{
			if (InputManager.ControlMode == InputManager.ControlType.Player)
			{
				m_cursorWorldPosition = worldPosition;

				// Use the point already calculated by the base class
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

					RefreshPlacementValidityVisuals(m_isPlacementValid);
					return;
				}

				if (m_isCancleHeld)
				{
					float selectionRadius = m_worldControllerManager.CursorVisualizer.SelectionRadius;
					int newColliders = Physics.OverlapSphereNonAlloc(worldPosition,
						selectionRadius,
						m_newCancelColliders,
						m_interactionLayer
					);

					// Check new colliders
					for (int i = 0; i < newColliders; i++)
					{
						Collider col = m_newCancelColliders[i];
						if (col == null)
							continue;

						if (col.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
							!blueprintCancelation.IsBeingCanceled)
						{
							blueprintCancelation.BeginCancelation();
						}
					}

					// Compare against the previous frame's tracked colliders
					for (int i = 0; i < m_collidersBeingCanceled.Length; i++)
					{
						Collider oldCol = m_collidersBeingCanceled[i];
						if (oldCol == null)
							continue;

						// Check if the old collider is present in the new set
						bool stillPresent = false;
						for (int j = 0; j < newColliders; j++)
						{
							if (m_newCancelColliders[j] == oldCol)
							{
								stillPresent = true;
								break;
							}
						}

						// If it was being canceled before, but is no longer touched by the cursor, stop it
						if (!stillPresent)
						{
							if (oldCol.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
								blueprintCancelation.IsBeingCanceled)
							{
								blueprintCancelation.StopCancelation();
							}
						}
					}

					Array.Clear(m_collidersBeingCanceled, 0, m_collidersBeingCanceled.Length);
					Array.Copy(m_newCancelColliders, m_collidersBeingCanceled, m_newCancelColliders.Length);
				}
				else
					StopAllCancelations();
			}
			else
				StopAllCancelations();
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

		private void TryPlaceBlueprint(Vector3 position, Quaternion rotation)
		{
			if (m_blueprintDataToPlace == null)
			{
				Debug.Log("[PlayerContsructionController] Tried to place a blueprint without blueprint data.",
					this);

				return;
			}

			int nearestSettlementID =
				SettlementManager.GetClosestSettlementID(transform.position, EFaction.FACTION_PLAYER);

			if (nearestSettlementID == -1)
				SettlementManager.Instance.CreatePlayerSettlement(position, out nearestSettlementID);

			if (!m_blueprintDataToPlace.IsMultiPointBlueprint)
			{
				CreateStructureBlueprint(nearestSettlementID, position, rotation);
			}
			else
			{
				CreateMultiPointBlueprint(nearestSettlementID, position, rotation);
			}
		}

		private void CreateStructureBlueprint(int nearestSettlementID, Vector3 position, Quaternion rotation)
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

		private void CreateMultiPointBlueprint(int nearestSettlementID, Vector3 position, Quaternion rotation)
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
				m_multiPointBlueprint.HandlePointAdded(position);
				m_currentPoints.Add(position);
			}

			m_pointsPlaced++;

			if (m_pointsPlaced >= m_blueprintDataToPlace.MaxPoints)
			{
				if (m_blueprintDataToPlace != null)
				{
					Transform blueprintTransform = m_multiPointBlueprint.Object.transform;
					if (blueprintTransform != null)
						GenerateMultiPointBlueprintLinks(blueprintTransform, m_blueprintDataToPlace, m_currentPoints);
				}

				ClearBlueprintToPlace();
			}
		}

		/// <summary>
		/// Generates and places mesh segments between multiple points along a path using specified spacing.
		/// </summary>
		private void GenerateMultiPointBlueprintLinks(Transform parentTransform, BlueprintData blueprintData, List<Vector3> points)
		{
			if (blueprintData == null || 
				blueprintData.PlacementMesh == null || 
				points.Count < 2)
				return;

			float spacing = blueprintData.MultiPointSpacing;
			Mesh blueprintMesh = blueprintData.PlacementMesh;

			// Loop through each link
			for (int i = 0; i < points.Count - 1; i++)
			{
				Vector3 start = points[i];
				Vector3 end = points[i + 1];

				Vector3 direction = end - start;
				float totalDistance = direction.magnitude;
				if (totalDistance <= 0.001f) 
					continue;
				direction.Normalize();

				int segmentCount = Mathf.FloorToInt(totalDistance / spacing);
				Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

				// Place each segment between this for this link
				for (int j = 0; j <= segmentCount; j++)
				{
					// Skip the first segment of the first link since it overlaps with the blueprint
					if (i == 0 && j == 0)
						continue;

					Vector3 spawnPosition = start + (direction * (j * spacing));

					GameObject segmentObject = new GameObject($"Segment_{i}_{j}");
					segmentObject.transform.SetParent(parentTransform, true);
					segmentObject.transform.position = spawnPosition;
					segmentObject.transform.rotation = pathRotation;

					MeshFilter filter = segmentObject.AddComponent<MeshFilter>();
					MeshRenderer renderer = segmentObject.AddComponent<MeshRenderer>();

					filter.mesh = blueprintMesh;
					renderer.sharedMaterial = m_blueprintMaterial;
				}
			}
		}

		private void RefreshPlacementValidityVisuals(bool isPlacementValid)
		{
			if(m_cursorVisualizer == null)
				return;

			if (m_blueprintDataToPlace == null)
			{
				m_cursorVisualizer.ClearBlueprintPlacementVisuals();
				m_cursorVisualizer.EnableCursor();
				return;
			}

			if (m_blueprintDataToPlace.IsMultiPointBlueprint && m_pointsPlaced > 0)
			{
				UpdateRealTimeMultiPointVisuals(isPlacementValid);
			}
			else // Update regular structure blueprint visuals
			{
				Material[] materials = { isPlacementValid ? m_validMaterial : m_invalidMaterial };

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
			if (m_pointsPlaced > 0)
			{
				// Rotate the first placed point's object to face the cursor
				if (m_pointsPlaced == 1)
				{
					Transform blueprintTransform = m_multiPointBlueprint.Object.transform;
					if (blueprintTransform != null && m_currentPoints.Count > 0)
					{
						Vector3 dirToCursor = m_cursorWorldPosition - m_currentPoints[0];
						if (dirToCursor.sqrMagnitude > 0.001f)
						{
							blueprintTransform.rotation = Quaternion.LookRotation(dirToCursor.normalized, Vector3.up);
						}
					}
				}

				// Update cursor visuals
				m_cursorVisualizer.SetMultiPointBlueprintPreviewVisuals(
					m_blueprintDataToPlace,
					m_currentPoints,
					m_cursorWorldPosition,
					materials
				);
			}
			else // First placed point visuals
			{
				m_cursorVisualizer.SetBlueprintPlacementVisuals(
					m_blueprintDataToPlace,
					materials,
					m_placementRotation
				);
			}
		}

		private void StopAllCancelations()
		{
			if (m_collidersBeingCanceled.Length > 0)
			{
				foreach (Collider i in m_collidersBeingCanceled)
				{
					if (i == null)
						continue;

					if (i.TryGetComponent(out BlueprintCancelation blueprintCancelation) &&
						blueprintCancelation.IsBeingCanceled)
					{
						blueprintCancelation.StopCancelation();
					}
				}
				Array.Clear(m_collidersBeingCanceled, 0, m_collidersBeingCanceled.Length);
			}
		}
	}
}