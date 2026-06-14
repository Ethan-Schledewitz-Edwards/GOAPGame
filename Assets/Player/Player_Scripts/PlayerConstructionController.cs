using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerConstructionController : PlayerWorldControllerBase, IInputHandler
{
	[SerializeField] private Material m_validMaterial;
	[SerializeField] private Material m_invalidMaterial;

	private StructureData m_blueprintStructureData;

	public event Action<StructureData, Vector3> PlacedStructure;

	protected override void Start()
	{
		base.Start();
		if (ConstructionManager.Instance != null)
			ConstructionManager.Instance.NewDevelopmentAttempted += SetBlueprint;

		enabled = false;
	}

	private void OnDestroy()
	{
		if (ConstructionManager.Instance != null)
			ConstructionManager.Instance.NewDevelopmentAttempted -= SetBlueprint;
	}

	#region Input Methods

	public override void Subscribe()
	{
		InputManager.Controls.Player.Look.performed += OnMouseInput;

		InputManager.Controls.Player.Primary.performed += OnPrimaryInput;

		InputManager.Controls.Player.Secondary.performed += OnSecondaryInput;
		InputManager.Controls.Player.Secondary.canceled += OnSecondaryInput;
	}

	public override void UnSubscribe()
	{
		InputManager.Controls.Player.Look.performed -= OnMouseInput;

		InputManager.Controls.Player.Primary.performed -= OnPrimaryInput;

		InputManager.Controls.Player.Secondary.performed -= OnSecondaryInput;
		InputManager.Controls.Player.Secondary.canceled -= OnSecondaryInput;
	}

	private void OnMouseInput(InputAction.CallbackContext context)
	{
		m_mousePosition = context.ReadValue<Vector2>();
	}

	private void OnPrimaryInput(InputAction.CallbackContext context)
	{
		TryPlaceBlueprint(m_cursorVisualizer.transform.position);
	}

	private void OnSecondaryInput(InputAction.CallbackContext context)
	{
		CancleBlueprintPlacement();
	}
	#endregion

	#region Actions

	private void TryPlaceBlueprint(Vector3 position)
	{
		if (m_blueprintStructureData == null)
			return;

		ConstructionManager.Instance.PlaceStructureBlueprint(m_blueprintStructureData, position);

		CancleBlueprintPlacement();
	}

	private void CancleBlueprintPlacement()
	{
		enabled = false;
		m_blueprintStructureData = null;
		m_cursorVisualizer.ReturnToDefaultVisuals();
	}
	#endregion

	private void SetBlueprint(StructureData structureData)
	{
		enabled = true;

		m_blueprintStructureData = structureData;
		Mesh mesh = m_blueprintStructureData.StructureBlueprintMesh;

		Material[] materials = new Material[1];
		materials[0] = m_validMaterial;

		Bounds bounds = mesh.bounds;
		Vector3 localOffset = new Vector3(0, bounds.extents.y, 0);

		m_cursorVisualizer.SetVisuals(structureData.StructureBlueprintMesh, materials, localOffset);
	}
}
