using TMPro;
using UISystems.RecyclingScrollRect;
using UnityEngine;

public class BlueprintOptionButton : MenuButton, IRecyclableCell<StructureBlueprintData>
{
	[SerializeField] private GameObject m_prefab;
	public GameObject Prefab => m_prefab;

	[SerializeField] private GameObject m_gameObject;
	public GameObject GameObject => m_gameObject;

	[SerializeField] private RectTransform m_rectTransform;
	public RectTransform RectTransform => m_rectTransform;

	[SerializeField] private TextMeshProUGUI m_blueprintTitleText;
	[SerializeField] private TextMeshProUGUI m_cellIDText;
	private StructureBlueprintData m_blueprintData;

	public void ConfigureCell(int index, StructureBlueprintData[] data)
	{
		if (m_blueprintTitleText)
			m_blueprintTitleText.text = data[index].DisplayName;

		if (m_cellIDText)
			m_cellIDText.text = index.ToString();

		m_blueprintData = data[index];
	}

	protected override void ButtonClicked()
	{
		base.ButtonClicked();

		MenuManager.CloseMenu(m_parentMenu);
		ConstructionManager.Instance.HandleBlueprintButton(m_blueprintData);
	}
}
