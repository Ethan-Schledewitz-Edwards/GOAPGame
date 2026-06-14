using TMPro;
using UISystems.RecyclingScrollRect;
using UnityEngine;

public class StructureOptionButton : MenuButton, IRecyclableCell<StructureData>
{
	[SerializeField] private GameObject m_prefab;
	public GameObject Prefab => m_prefab;

	[SerializeField] private GameObject m_gameObject;
	public GameObject GameObject => m_gameObject;

	[SerializeField] private RectTransform m_rectTransform;
	public RectTransform RectTransform => m_rectTransform;

	[SerializeField] private TextMeshProUGUI m_structureTitleText;
	[SerializeField] private TextMeshProUGUI m_cellIDText;
	private StructureData m_structureData;

	public void ConfigureCell(int index, StructureData[] data)
	{
		if (m_structureTitleText)
			m_structureTitleText.text = data[index].DisplayName;

		if (m_cellIDText)
			m_cellIDText.text = index.ToString();

		m_structureData = data[index];
	}

	protected override void ButtonClicked()
	{
		base.ButtonClicked();

		MenuManager.CloseMenu(m_parentMenu);
		ConstructionManager.Instance.HandleBlueprintButton(m_structureData);
	}
}
