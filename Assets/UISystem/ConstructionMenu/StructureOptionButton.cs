using TMPro;
using UISystems.RecyclingScrollRect;
using UnityEngine;

public class StructureOptionButton : MonoBehaviour, IRecyclableCell<StructureData>
{
	private RectTransform m_rectTransform;
	public RectTransform RectTransform => m_rectTransform;

	private GameObject m_gameObject;
	public GameObject GameObject => m_gameObject;

	[SerializeField] private TextMeshProUGUI m_structureTitleText;
	[SerializeField] private TextMeshProUGUI m_cellIDText;

	public void ConfigureCell(int index, StructureData[] data)
	{
		if (m_structureTitleText)
			m_structureTitleText.text = data[index].DisplayName;

		if (m_cellIDText)
			m_cellIDText.text = index.ToString();
	}
}
