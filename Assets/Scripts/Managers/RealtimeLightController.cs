using System;
using UnityEngine;
using WorldLighting;

public class RealtimeLightController : MonoBehaviour, ITimeOfDayController
{
	public event Action<int, int> ClockUpdated;

	void Update()
	{
		(int currentHour, int currentMinute) = GetTimeOfDay();
		ClockUpdated?.Invoke(currentHour, currentMinute);
	}

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
