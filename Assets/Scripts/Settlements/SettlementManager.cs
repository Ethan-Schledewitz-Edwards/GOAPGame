using System.Collections.Generic;
using UnityEngine;

public class SettlementManager : MonoBehaviour
{
	public static SettlementManager Instance;

	[SerializeField] Settlement[] m_SettlementsTemp;// Remove this when the player can define settlements

	public Dictionary<int, Settlement> WorldSettlements;

	private void Awake()
	{
		for (int i = 0; i < m_SettlementsTemp.Length; i++)
		{
			Settlement settlement = m_SettlementsTemp[i];
			WorldSettlements.Add(i, settlement);
		}

		if (Instance == null)
			Instance = this;
		else 
			Destroy(this);
	}
}
