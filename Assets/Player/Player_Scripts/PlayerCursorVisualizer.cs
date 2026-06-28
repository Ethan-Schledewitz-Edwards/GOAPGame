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
	[SerializeField] private Material[] m_blueprintMaterial;

	private Coroutine m_shrinkCoroutine;
	private Coroutine m_growCoroutine;

	private void Awake()
	{
		ResetBlueprintVisuals();
		ResetCursorVisuals();
	}

	private void OnDisable()
	{
		DisableCursor();
	}

	public void SetVisualsPosition(Vector3 position)
	{
		transform.position = position;
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

	public void ResetCursorVisuals()
	{
		m_cursorObject.transform.localScale = m_cursorMinTargetScale;
	}

	public void ResetBlueprintVisuals()
	{
		m_meshFilter.mesh = m_defaultMesh;
		m_meshRenderer.sharedMaterials = m_blueprintMaterial;
		m_meshRenderer.transform.localPosition = Vector3.zero;
		m_meshRenderer.transform.localRotation = Quaternion.identity;
	}

	public void GrowCursor()
	{
		if(m_shrinkCoroutine != null)
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

	#region

	public void SetBlueprint(Mesh mesh, Material[] materials, Vector3 localOffset = default, Quaternion localRotation = default)
	{
		m_cursorObject.SetActive(false);

		m_meshFilter.mesh = mesh;
		m_meshRenderer.sharedMaterials = materials;
		m_meshRenderer.transform.localPosition = localOffset;
		m_meshRenderer.transform.rotation = localRotation;
		m_blueprintVisualsObject.SetActive(true);
	}

	private void DisableBlueprint()
	{
		m_blueprintVisualsObject.SetActive(false);
	}
	#endregion
}
