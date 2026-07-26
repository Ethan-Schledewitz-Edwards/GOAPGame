using System;
using UnityEngine;

namespace Managers
{
	public class MissionClock : MonoBehaviour, IGameClock
	{
		public event Action<int, int> ClockUpdated;
		public event Action<float> TimeOfDayFractionUpdated;

		public (int, int) GetTimeOfDay()
		{
			throw new System.NotImplementedException();
		}

		public float GetTimeOfDayFraction()
		{
			throw new System.NotImplementedException();
		}
	}
}
