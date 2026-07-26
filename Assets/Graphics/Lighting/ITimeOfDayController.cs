using System;
using UnityEngine;

namespace WorldLighting
{
    public interface ITimeOfDayController
    {
		public event Action<int, int> ClockUpdated;

		public float GetTimeOfDayFraction()
		{
			DateTime currentTime = DateTime.Now;
			float fractionOfDay = currentTime.Hour / 24f + currentTime.Minute / 1440f;
			return fractionOfDay;
		}

		public (int, int) GetTimeOfDay()
		{
			DateTime currentTime = DateTime.Now;
			return (currentTime.Hour, currentTime.Minute);
		}
	}
}
