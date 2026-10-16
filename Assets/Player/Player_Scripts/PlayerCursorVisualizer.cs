using Construction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCursorVisualizer : MonoBehaviour
{
	private const float c_cursorGrowTime = .7f;
	private const float c_maxSelectionRadius = 2f;
	private const float c_cursorShrinkTime = 0.1f;
	private const float c_minSelectionRadius = .1f;

	[Header("Cursor")]
	[SerializeField] private GameObject m_cursorObject;
	private Vector3 m_cursorMinTargetScale = new Vector3(.07f, .07f, .07f);
	private Vector3 m_cursorMaxTargetScale = new Vector3(.4f, .4f, .4f);
	public float SelectionRadius { get; private set; }

	[Header("Blueprint")]
	[SerializeField] private GameObject m_blueprintVisualsObject;
	[SerializeField] private MeshRenderer m_meshRenderer;
	[SerializeField] private MeshFilter m_meshFilter;

	[Header("Default Values")]
	[SerializeField] private Mesh m_defaultMesh;
	[SerializeField] public Material[] m_blueprintMaterial; // Blue blueprint material

	private List<GameObject> m_activePreviewInstances = new List<GameObject>();

	private Coroutine m_shrinkCoroutine;
	private Coroutine m_growCoroutine;

	private void Awake()
	{
		m_cursorObject.transform.localScale = m_cursorMinTargetScale;
		ClearBlueprintPlacementVisuals();
	}

	private void OnDisable()
	{
		DisableCursor();
	}

	public void SetVisualsPosition(Vector3 position)
	{
		transform.position = position;
	}

	public void SetVisualsRotation(Quaternion rotation)
	{
		transform.rotation = rotation;
	}

	#region Cursor

	public void EnableCursor()
	{
		m_blueprintVisualsObject.SetActive(false);
		m_cursorObject.SetActive(true);
	}

	public void DisableCursor()
	{
		m_cursorObject.SetActive(false);
	}

	public void GrowCursor()
	{
		if (m_shrinkCoroutine != null)
			StopCoroutine(m_shrinkCoroutine);

		if (m_growCoroutine != null)
			StopCoroutine(m_growCoroutine);

		m_growCoroutine = StartCoroutine(GrowCursorOverTime());
	}

	public void ShrinkCursor()
	{
		if (m_shrinkCoroutine != null)
			StopCoroutine(m_shrinkCoroutine);

		if (m_growCoroutine != null)
			StopCoroutine(m_growCoroutine);

		m_growCoroutine = StartCoroutine(ShrinkCursorOverTime());
	}

	private IEnumerator GrowCursorOverTime()
	{
		float timeGrowing = 0f;
		Vector3 startScale = m_cursorObject.transform.localScale;
		float startRadius = SelectionRadius;

		while (timeGrowing < c_cursorGrowTime)
		{
			float t = timeGrowing / c_cursorGrowTime;

			m_cursorObject.transform.localScale = Vector3.Lerp(startScale, m_cursorMaxTargetScale, t);
			SelectionRadius = Mathf.Lerp(startRadius, c_maxSelectionRadius, t);

			timeGrowing += Time.deltaTime;
			yield return null;
		}

		SelectionRadius = c_maxSelectionRadius;
		m_cursorObject.transform.localScale = m_cursorMaxTargetScale;
	}

	private IEnumerator ShrinkCursorOverTime()
	{
		float timeShrinking = 0f;
		Vector3 startScale = m_cursorObject.transform.localScale;
		float startRadius = SelectionRadius;

		while (timeShrinking < c_cursorShrinkTime)
		{
			float t = timeShrinking / c_cursorShrinkTime;

			m_cursorObject.transform.localScale = Vector3.Lerp(startScale, m_cursorMinTargetScale, t);
			SelectionRadius = Mathf.Lerp(startRadius, c_minSelectionRadius, t);

			timeShrinking += Time.deltaTime;
			yield return null;
		}

		SelectionRadius = c_minSelectionRadius;
		m_cursorObject.transform.localScale = m_cursorMinTargetScale;
	}
	#endregion

	public void SetBlueprintPlacementVisuals(BlueprintData blueprintData,
		Material[] materials,
		Quaternion localRotation = default)
	{
		ClearBlueprintPlacementVisuals();
		m_cursorObject.SetActive(false);

		if (blueprintData == null || blueprintData.PlacementMesh == null)
			return;

		Mesh blueprintMesh = blueprintData.PlacementMesh;
		Bounds blueprintBounds = blueprintMesh.bounds;

		m_meshFilter.mesh = blueprintMesh;
		m_meshRenderer.sharedMaterials = materials;
		m_meshRenderer.transform.rotation = localRotation;

		float distanceToBottom = blueprintBounds.center.y - blueprintBounds.extents.y;
		m_meshRenderer.transform.localPosition = Vector3.up * distanceToBottom;

		m_blueprintVisualsObject.SetActive(true);
	}

	public void SetMultiPointBlueprintPreviewVisuals(BlueprintData blueprintData, List<Vector3> confirmedPoints, Vector3 cursorPosition, Material[] previewMaterials)
	{
		ClearBlueprintPlacementVisuals();
		m_cursorObject.SetActive(false);

		if (blueprintData == null || blueprintData.PlacementMesh == null || confirmedPoints.Count == 0)
			return;

		float spacing = blueprintData.MultiPointSpacing > 0 ? blueprintData.MultiPointSpacing : Mathf.Max(blueprintData.PlacementMesh.bounds.size.z, 2f);
		Mesh blueprintMesh = blueprintData.PlacementMesh;
		Bounds blueprintBounds = blueprintMesh.bounds;
		float distanceToBottom = blueprintBounds.center.y - blueprintBounds.extents.y;

		m_blueprintVisualsObject.SetActive(true);

		// 1. Draw confirmed segments using blue blueprint material
		for (int i = 0; i < confirmedPoints.Count - 1; i++)
		{
			DrawSegment(blueprintData, confirmedPoints[i], confirmedPoints[i + 1], spacing, blueprintMesh, distanceToBottom, m_blueprintMaterial);
		}

		// 2. Draw live preview segment stretching to cursor using green/red validation materials
		if (confirmedPoints.Count > 0)
		{
			Vector3 lastConfirmed = confirmedPoints[confirmedPoints.Count - 1];
			DrawSegment(blueprintData, lastConfirmed, cursorPosition, spacing, blueprintMesh, distanceToBottom, previewMaterials);
		}
	}

	private void DrawSegment(BlueprintData blueprintData, Vector3 start, Vector3 end, float spacing, Mesh blueprintMesh, float distanceToBottom, Material[] materials)
	{
		Vector3 direction = end - start;
		float totalDistance = direction.magnitude;
		if (totalDistance <= 0.001f) return;

		direction.Normalize();
		int postCount = Mathf.FloorToInt(totalDistance / spacing);
		Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

		for (int j = 0; j <= postCount; j++)
		{
			Vector3 spawnPos = start + (direction * (j * spacing));

			GameObject previewObj = new GameObject($"PreviewFenceMesh_{m_activePreviewInstances.Count}");
			previewObj.transform.SetParent(m_blueprintVisualsObject.transform, true);
			previewObj.transform.position = spawnPos + (pathRotation * new Vector3(0, distanceToBottom, 0));
			previewObj.transform.rotation = pathRotation;

			MeshFilter filter = previewObj.AddComponent<MeshFilter>();
			MeshRenderer renderer = previewObj.AddComponent<MeshRenderer>();

			filter.mesh = blueprintMesh;
			renderer.sharedMaterials = materials;

			m_activePreviewInstances.Add(previewObj);
		}
	}

	public void GeneratePermanentSegments(Transform parentTransform, BlueprintData blueprintData, List<Vector3> points)
	{
		if (blueprintData == null || blueprintData.PlacementMesh == null || points.Count < 2)
			return;

		float spacing = blueprintData.MultiPointSpacing > 0 ? blueprintData.MultiPointSpacing : Mathf.Max(blueprintData.PlacementMesh.bounds.size.z, 2f);
		Mesh blueprintMesh = blueprintData.PlacementMesh;
		Bounds blueprintBounds = blueprintMesh.bounds;
		float distanceToBottom = blueprintBounds.center.y - blueprintBounds.extents.y;

		for (int i = 0; i < points.Count - 1; i++)
		{
			Vector3 start = points[i];
			Vector3 end = points[i + 1];

			Vector3 direction = end - start;
			float totalDistance = direction.magnitude;
			if (totalDistance <= 0.001f) continue;

			direction.Normalize();
			int postCount = Mathf.FloorToInt(totalDistance / spacing);
			Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

			for (int j = 0; j <= postCount; j++)
			{
				Vector3 spawnPos = start + (direction * (j * spacing));

				GameObject permanentObj = new GameObject($"FenceSegment_{i}_{j}");
				permanentObj.transform.SetParent(parentTransform, true);
				permanentObj.transform.position = spawnPos + (pathRotation * new Vector3(0, distanceToBottom, 0));
				permanentObj.transform.rotation = pathRotation;

				MeshFilter filter = permanentObj.AddComponent<MeshFilter>();
				MeshRenderer renderer = permanentObj.AddComponent<MeshRenderer>();

				filter.mesh = blueprintMesh;
				// Permanent segments always use the blue blueprint material
				renderer.sharedMaterials = m_blueprintMaterial;
			}
		}
	}

	public void ClearBlueprintPlacementVisuals()
	{
		m_blueprintVisualsObject.SetActive(false);
		m_meshFilter.mesh = m_defaultMesh;
		m_meshRenderer.sharedMaterials = m_blueprintMaterial;
		m_meshRenderer.transform.localPosition = Vector3.zero;
		m_meshRenderer.transform.localRotation = Quaternion.identity;

		foreach (var obj in m_activePreviewInstances)
		{
			if (obj != null)
				Destroy(obj);
		}
		m_activePreviewInstances.Clear();
	}
}