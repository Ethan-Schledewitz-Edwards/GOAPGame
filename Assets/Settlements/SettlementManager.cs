using System;
using System.Collections.Generic;
using UnityEngine;
using Factions.Core;

namespace Settlements 
{
	public class SettlementManager : MonoBehaviour
	{
		public static SettlementManager Instance;

		public event Action<int> PlayerSettlementCreated;

		public static Dictionary<int, Settlement> s_WorldSettlements = new Dictionary<int, Settlement>();

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else
				Destroy(this);
		}

		public void CreateNewSettlement(Vector3 position, ESettlementFaction settlementFaction, out int id)
		{
			id = s_WorldSettlements.Count + 1;
			Settlement settlement = new Settlement(id, settlementFaction);
			s_WorldSettlements[id] = settlement;
		}

		public void CreateWorldSettelemnt(Vector3 position, out int id)
		{
			CreateNewSettlement(position, ESettlementFaction.FACTION_WORLD, out id);
			PlayerSettlementCreated?.Invoke(id);
		}

		public void CreatePlayerSettlement(Vector3 position, out int id)
		{
			CreateNewSettlement(position, ESettlementFaction.FACTION_PLAYER, out id);
			PlayerSettlementCreated?.Invoke(id);
		}

		public void CreateEnemySettlement(Vector3 position, out int id)
		{
			CreateNewSettlement(position, ESettlementFaction.FACTION_ENEMY, out id);
			PlayerSettlementCreated?.Invoke(id);
		}

		public static Settlement GetClosestSettlement(Vector3 position, ESettlementFaction factionOfSettlement)
		{
			if (SettlementManager.s_WorldSettlements == null || SettlementManager.s_WorldSettlements.Count == 0)
				return null;

			Settlement closestSettlement = null;
			float closestDistanceSqr = Mathf.Infinity;

			foreach (var i in SettlementManager.s_WorldSettlements)
			{
				Settlement settlement = i.Value;

				if (settlement.SettlementFaction != factionOfSettlement)
					continue;

				Vector3 settlementPos = settlement.GetSettlementCenter();

				float distToSettlementSqr = (settlementPos - position).sqrMagnitude;
				if (distToSettlementSqr < closestDistanceSqr)
				{
					closestDistanceSqr = distToSettlementSqr;
					closestSettlement = settlement;
				}
			}

			return closestSettlement;
		}

		public static int GetClosestSettlementID(Vector3 position, ESettlementFaction factionOfSettlement)
		{
			Settlement closest = GetClosestSettlement(position, factionOfSettlement);
			return closest != null ? closest.SettlementID : -1;
		}
	}
}