using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	[field: SerializeField] public GameObject PlayerObject { get; private set; }

	public event Action<int, int> ClockUpdated;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);

		//SpawnPlayer();
	}

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
