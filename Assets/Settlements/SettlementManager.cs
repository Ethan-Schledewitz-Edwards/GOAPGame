using System;
using System.Collections.Generic;
using UnityEngine;

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

		public void CreateNewSettlement(Vector3 position, bool isFriendly, bool isBuildable, out int id)
		{
			id = s_WorldSettlements.Count + 1;
			Settlement settlement = new Settlement(id, isFriendly, isBuildable);
			s_WorldSettlements[id] = settlement;
		}

		public void CreatePlayerSettlement(Vector3 position, out int id)
		{
			CreateNewSettlement(position, true, true, out id);
			PlayerSettlementCreated?.Invoke(id);
		}

		public static Settlement GetClosestSettlement(Vector3 position, bool isBuildable, bool isFriendly)
		{
			if (SettlementManager.s_WorldSettlements == null || SettlementManager.s_WorldSettlements.Count == 0)
				return null;

			Settlement closestSettlement = null;
			float closestDistanceSqr = Mathf.Infinity;

			foreach (var i in SettlementManager.s_WorldSettlements)
			{
				Settlement settlement = i.Value;

				if (settlement.IsSettlementFriendly != isFriendly ||
					settlement.IsSettlementBuildable != isBuildable)
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

		public static int GetClosestSettlementID(Vector3 position, bool isBuildable, bool isFriendly)
		{
			Settlement closest = GetClosestSettlement(position, isBuildable, isFriendly);
			return closest != null ? closest.SettlementID : -1;
		}
	}

}