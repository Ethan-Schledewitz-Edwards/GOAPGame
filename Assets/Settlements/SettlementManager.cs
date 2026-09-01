using Factions.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Settlements 
{
	public class SettlementManager : MonoBehaviour
	{
		public static SettlementManager Instance;

		public static Dictionary<int, Settlement> s_WorldSettlements = new Dictionary<int, Settlement>();

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else
				Destroy(this);

			// Create the default settlement for nuetral structures to populate
			CreateNewSettlement(Vector3.zero, EFaction.FACTION_WORLD, out _);
		}

		public void CreateNewSettlement(Vector3 position, EFaction settlementFaction, out int id)
		{
			bool isDefaultFaction = settlementFaction == EFaction.FACTION_WORLD;

			// Only one world settlement is allowed
			if (isDefaultFaction &&
				s_WorldSettlements.Count > 0 &&
				s_WorldSettlements[0].SettlementFaction == EFaction.FACTION_WORLD)
			{
				id = -1;
				return;
			}

			id = isDefaultFaction ? 0 : s_WorldSettlements.Count + 1;
			Settlement settlement = new Settlement(id, settlementFaction);
			s_WorldSettlements[id] = settlement;
		}

		public void CreatePlayerSettlement(Vector3 position, out int id)
		{
			CreateNewSettlement(position, EFaction.FACTION_PLAYER, out id);
		}

		public void CreateEnemySettlement(Vector3 position, out int id)
		{
			CreateNewSettlement(position, EFaction.FACTION_ENEMY, out id);
		}

		public static Settlement GetClosestSettlement(Vector3 position, EFaction factionOfSettlement)
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

		public static int GetClosestSettlementID(Vector3 position, EFaction factionOfSettlement)
		{
			Settlement closest = GetClosestSettlement(position, factionOfSettlement);
			return closest != null ? closest.SettlementID : -1;
		}
	}
}