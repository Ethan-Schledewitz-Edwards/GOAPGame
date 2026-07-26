using System;
using UnityEngine;

namespace Managers
{
	public interface IGameClock
	{
		public event Action<int, int> ClockUpdated;
		public event Action<float> TimeOfDayFractionUpdated;

		public abstract float GetTimeOfDayFraction();

		public abstract (int, int) GetTimeOfDay();
	}
}
