using System;
using UnityEngine;

namespace Managers
{
	public class RealtimeClock : MonoBehaviour, IGameClock
	{
		public event Action<int, int> ClockUpdated;
		public event Action<float> TimeOfDayFractionUpdated;

		private int m_lastHour = -1;
		private int m_lastMinute = -1;

		private void Update()
		{
			DateTime now = DateTime.Now;

			float fraction = (float)now.TimeOfDay.TotalDays;
			TimeOfDayFractionUpdated?.Invoke(fraction);

			if (now.Hour != m_lastHour || 
				now.Minute != m_lastMinute)
			{
				m_lastHour = now.Hour;
				m_lastMinute = now.Minute;
				ClockUpdated?.Invoke(m_lastHour, m_lastMinute);
			}
		}

		public float GetTimeOfDayFraction()
		{
			return (float)DateTime.Now.TimeOfDay.TotalDays;
		}

		public (int, int) GetTimeOfDay()
		{
			DateTime now = DateTime.Now;
			return (now.Hour, now.Minute);
		}
	}
}
