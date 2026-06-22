using UnityEngine;

public class PlayerCursorVisualizer : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private MeshRenderer m_meshRenderer;
	[SerializeField] private MeshFilter m_meshFilter;

	[Header("Default Values")]
	[SerializeField] private Mesh m_defaultMesh;
	[SerializeField] private Material[] m_defaultMaterial;

	private void Awake()
	{
		ReturnToDefaultVisuals();
	}

	public void SetPosition(Vector3 position)
	{
		transform.position = position;
	}

	public void SetVisuals(Mesh mesh, Material[] materials, Vector3 localOffset = default, Quaternion localRotation = default)
	{
		m_meshFilter.mesh = mesh;
		m_meshRenderer.sharedMaterials = materials;
		m_meshRenderer.transform.localPosition = localOffset;
		m_meshRenderer.transform.rotation = localRotation;
	}

	public void ReturnToDefaultVisuals()
	{
		m_meshFilter.mesh = m_defaultMesh;
		m_meshRenderer.sharedMaterials = m_defaultMaterial;
		m_meshRenderer.transform.localPosition = Vector3.zero;
		m_meshRenderer.transform.localRotation = Quaternion.identity;
	}
}
