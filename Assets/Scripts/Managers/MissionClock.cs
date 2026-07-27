using System;
using UnityEngine;

namespace Managers
{
	public class MissionClock : MonoBehaviour, IGameClock
	{
		private const int c_secondInMin = 60;
		private const int c_minInHour = 60;
		private const int c_hourInDay = 24;
		private const int c_totalMinutesInDay = c_hourInDay * c_minInHour;

		[Header("Clock Properties")]
		[Tooltip("How many in-game minutes pass for every 1 real-life second.")]
		[SerializeField] private float m_minutesPerSecond = 2f;

		[Header("Day / Night Thresholds (Hours)")]
		[SerializeField] private int m_sunriseHour = 6;
		[SerializeField] private int m_sunsetHour = 19;

		public event Action<int, int> ClockUpdated;
		public event Action<float> TimeOfDayFractionUpdated;
		public event Action ClockFinished;
		public event Action SunHasRisen;
		public event Action SunHasSet;

		// System
		private bool m_isClockActive = false;
		private float m_time;
		private int m_day;
		private int m_daysInMission;

		private int m_previousMinute = -1;
		private int m_previousHour = -1;
		private bool m_isDaytime;


		public (int, int) GetTimeOfDay()
		{
			int totalGameMinutes = Mathf.FloorToInt(m_time * c_totalMinutesInDay);
			int hour = (totalGameMinutes / c_minInHour) % c_hourInDay;
			int minute = totalGameMinutes % c_minInHour;

			return (hour, minute);
		}

		public float GetTimeOfDayFraction()
		{
			return Mathf.Clamp01(m_time);
		}

		private void Update()
		{
			if (m_isClockActive)
			{
				IncrementTime();
			}
		}

		/// <summary>
		/// Initializes the mission clock then begins counting up
		/// </summary>
		public void StartClock(int daysInMission)
		{
			m_daysInMission = daysInMission;
			m_day = 0;
			m_time = 0f;

			m_previousMinute = -1;
			m_previousHour = -1;

			m_isClockActive = true;
		}

		/// <summary>
		/// Pauses the mission clock.
		/// </summary>
		public void PauseClock()
		{
			m_isClockActive = false;
		}

		/// <summary>
		/// Resumes the mission clock
		/// </summary>
		public void ResumeClock()
		{
			m_isClockActive = true;
		}

		/// <summary>
		/// Stops the mission clock from ticking and resets all stored variables
		/// </summary>
		public void StopClock()
		{
			m_isClockActive = false;
			m_day = 0;
			m_time = 0f;

			m_previousMinute = -1;
			m_previousHour = -1;
		}

		private void IncrementTime()
		{
			// Convert real-time seconds to in-game minute fraction
			// 1 real second = m_minutesPerSecond game minutes
			float deltaMinutes = Time.deltaTime * m_minutesPerSecond;

			// Increment normalized day progression (1 day = 1440 minutes)
			m_time += deltaMinutes / c_totalMinutesInDay;

			// Fire fraction update event for lighting/skybox controllers
			TimeOfDayFractionUpdated?.Invoke(GetTimeOfDayFraction());

			var (hour, minute) = GetTimeOfDay();

			// Check for minute changes
			if (minute != m_previousMinute || hour != m_previousHour)
			{
				m_previousMinute = minute;
				m_previousHour = hour;

				ClockUpdated?.Invoke(hour, minute);
				CheckSunEvents(hour);
			}

			// Check for day roll-over
			if (m_time >= 1.0f)
			{
				m_time -= 1.0f; // Preserve overflow fractional precision
				m_day++;

				if (m_day >= m_daysInMission)
				{
					StopClock();
					ClockFinished?.Invoke();
				}
			}
		}

		private void CheckSunEvents(int currentHour)
		{
			bool isCurrentlyDay = currentHour >= m_sunriseHour && currentHour < m_sunsetHour;

			if (isCurrentlyDay && !m_isDaytime)
			{
				m_isDaytime = true;
				SunHasRisen?.Invoke();
			}
			else if (!isCurrentlyDay && m_isDaytime)
			{
				m_isDaytime = false;
				SunHasSet?.Invoke();
			}
		}

		public int GetRemainingHoursInDay()
		{
			var (hour, _) = GetTimeOfDay();
			return c_hourInDay - hour;
		}

		public int GetHoursInDay()
		{
			return c_hourInDay;
		}

		public int GetMinInHour()
		{
			return c_minInHour;
		}
	}
}
