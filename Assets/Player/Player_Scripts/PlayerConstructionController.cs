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
		[SerializeField] private PlayerCamera m_playerCamera;
		[SerializeField] private PlayerCursorVisualizer m_cursorVisualizer;
		private PlayerFollowerController m_followerController;

		public override event Action<Transform> CursorAnchorSet;
		public override event Action ResetCursorAnchor;

		// System
		private bool m_isCancleHeld = false;

		private Vector3 m_cursorWorldPosition;
		private Vector3 m_groundNormal;

		private bool m_isPlacementValid;
		private BlueprintData m_blueprintDataToPlace;
		private Quaternion m_placementRotation;

		// Multi point blueprint
		private ILinkBlueprintObject m_linkBlueprint;

		Collider[] m_newCancelColliders = new Collider[16];
		Collider[] m_collidersBeingCanceled = new Collider[16];

		#region Lifecycle

		private void Awake()
		{
			if (m_playerCamera == null)
				Debug.LogWarning("[PlayerConstructionController] No reference to the player camera was set.", this);

			if (m_blockingLayer == 0) 
				m_blockingLayer = LayerMask.GetMask("Player", "Actor");

			if (m_interactionLayer == 0) 
				m_interactionLayer = LayerMask.GetMask("Interaction");

			m_followerController = GetComponent<PlayerFollowerController>();
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
				// Destroy any unfinished link
				if(m_linkBlueprint != null)
				{
					Destroy(m_linkBlueprint.Object);
				}

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

		public override void RefreshCursor(Vector3 worldPoint, Vector3 normal)
		{
			if (InputManager.ControlMode == InputManager.ControlType.Player)
			{
				m_cursorWorldPosition = worldPoint;
				m_groundNormal = normal;

				LayerMask combinedCheckMask = m_blockingLayer | m_interactionLayer;
				if (m_blueprintDataToPlace != null)
				{
					Vector3 extents = m_blueprintDataToPlace.PlacementMesh.bounds.extents;
					float baseRadius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
					float clearance = baseRadius + m_blueprintDataToPlace.PlacementClearenceRadius;

					bool isBlocked = Physics.CheckSphere(
						m_cursorWorldPosition,
						clearance,
						combinedCheckMask
					);

					bool isNormalSafe = IsPlacementNormalSafe(normal, m_blueprintDataToPlace.MaxPlacementAngle);

					m_isPlacementValid = !isBlocked && isNormalSafe;

					RefreshPlacementValidityVisuals(m_isPlacementValid);
					return;
				}

				if (m_isCancleHeld)
				{
					float selectionRadius = m_cursorVisualizer.SelectionRadius;
					int newColliders = Physics.OverlapSphereNonAlloc(m_cursorWorldPosition,
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

		/// <summary>
		/// Sets the blueprint data to be placed and disables the follower controller.
		/// </summary>
		private void SetBlueprintToPlace(BlueprintData structureData)
		{
			ClearBlueprintToPlace();

			if (m_followerController != null)
				m_followerController.enabled = false;

			m_blueprintDataToPlace = structureData;
		}

		/// <summary>
		/// Resets blueprint placement state, clears cursor visuals, then enables the follower controller.
		/// </summary>
		private void ClearBlueprintToPlace()
		{
			m_playerCamera.ReturnToDefault();
			ResetCursorAnchor?.Invoke();

			m_isPlacementValid = false;
			m_blueprintDataToPlace = null;
			m_placementRotation = Quaternion.identity;

			m_linkBlueprint = null;

			if(m_cursorVisualizer != null)
			{
				m_cursorVisualizer.ClearBlueprintPlacementVisuals();
				m_cursorVisualizer.EnableCursor();
				RefreshPlacementValidityVisuals(false);
			}

			if (m_followerController != null)
				m_followerController.enabled = true;
		}

		/// <summary>
		/// Attempts to place a blueprint at the specified position and rotation, creating a new player settlement if
		/// necessary.
		/// </summary>
		private void TryPlaceBlueprint(Vector3 position, Quaternion rotation)
		{
			if (m_blueprintDataToPlace == null)
			{
				Debug.Log("[PlayerConstructionController] Tried to place a blueprint without blueprint data.", this);
				return;
			}

			int nearestSettlementID =
				SettlementManager.GetClosestSettlementID(transform.position, EFaction.FACTION_PLAYER);

			if (nearestSettlementID == -1)
				SettlementManager.Instance.CreatePlayerSettlement(position, out nearestSettlementID);

			if (!m_blueprintDataToPlace.IsLinkBlueprint)
			{
				CreateStructureBlueprint(nearestSettlementID, position, rotation);
			}
			else
			{
				CreateLinkBlueprint(nearestSettlementID, position, rotation);
			}
		}

		/// <summary>
		/// Creates a standard blueprint structure at the specified position and rotation, 
		/// assigning it to the nearest settlement.
		/// </summary>
		private void CreateStructureBlueprint(int nearestSettlementID, Vector3 position, Quaternion rotation)
		{
			ConstructionManager constructionManager = ConstructionManager.Instance;
			constructionManager.CreateBlueprintPreview
				(
					m_blueprintDataToPlace,
					position,
					out IBlueprintObject blueprintObject
				);

			constructionManager.PlacePreviewBlueprint(nearestSettlementID, m_blueprintDataToPlace, position, rotation, blueprintObject);

			ClearBlueprintToPlace();
		}

		/// <summary>
		/// Creates a link blueprint preview or finalizes and places an existing link blueprint at the specified position
		/// and rotation, assigning it to the nearest settlement.
		/// </summary>
		private void CreateLinkBlueprint(int nearestSettlementID, Vector3 position, Quaternion rotation)
		{
			ConstructionManager constructionManager = ConstructionManager.Instance;

			if (m_linkBlueprint == null)
			{
				// Create a blueprint preview
				constructionManager.CreateBlueprintPreview
				(
					m_blueprintDataToPlace,
					position,
					out IBlueprintObject blueprintPreview
				);

				m_linkBlueprint = blueprintPreview as ILinkBlueprintObject;
				if (m_linkBlueprint != null)
				{
					m_linkBlueprint.SetLinkStartPosition(position);
				}

				// Update camera target
				if (m_linkBlueprint?.Object != null)
				{
					m_playerCamera.ClearCameraTargets();
					m_playerCamera.SetCameraSmoothing(4f);
					m_playerCamera.AddCameraTarget(m_linkBlueprint.Object.transform);
					m_playerCamera.AddCameraTarget(m_cursorVisualizer.transform);

					CursorAnchorSet?.Invoke(m_linkBlueprint.Object.transform);
				}
			}
			else // Create a link
			{
				// Add the end point
				m_linkBlueprint.SetLinkEndPosition(position);

				// Place the link
				constructionManager.PlacePreviewBlueprint(nearestSettlementID, 
					m_blueprintDataToPlace,
					m_linkBlueprint.LinkStartPosition, 
					rotation, 
					m_linkBlueprint
				);

				// Start another link
				m_linkBlueprint = null;
				CreateLinkBlueprint(nearestSettlementID, position, rotation);
			}
		}

		private void RefreshPlacementValidityVisuals(bool isPlacementValid)
		{
			if (m_cursorVisualizer == null || m_blueprintDataToPlace == null)
				return;

			Material[] materials = { isPlacementValid ? m_validMaterial : m_invalidMaterial };

			// Update linked blueprint preivew visuals
			if (m_blueprintDataToPlace.IsLinkBlueprint && 
				m_linkBlueprint != null)
			{
				Transform blueprintTransform = m_linkBlueprint.Object?.transform;
				Vector3 startPosition = m_linkBlueprint.LinkStartPosition;

				Vector3 dirToCursor = m_cursorWorldPosition - startPosition;
				if (dirToCursor.sqrMagnitude > 0.001f)
				{
					blueprintTransform.rotation = Quaternion.LookRotation(dirToCursor.normalized, Vector3.up);
				}

				m_cursorVisualizer.SetLinkedBlueprintPreviewVisuals(
					m_blueprintDataToPlace,
					startPosition,
					m_cursorWorldPosition,
					materials
				);
			}
			else // Update regular blueprint preivew visuals
			{
				m_cursorVisualizer.SetBlueprintPreviewVisuals(
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

		private bool IsPlacementNormalSafe(Vector3 groundNormal, float maxAngle)
		{
			float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
			return slopeAngle <= maxAngle;
		}
	}
}