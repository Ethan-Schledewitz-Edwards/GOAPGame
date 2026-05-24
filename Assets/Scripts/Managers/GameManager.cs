using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	[field: SerializeField] public GameObject PlayerObject { get; private set; }

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);

		//SpawnPlayer();
	}

	/*
	void Update()
	{
		Debug.Log("Time of Day (fraction): " + GetTimeOfDayFract());
	}
	*/

	public float GetTimeOfDayFract()
	{
		DateTime currentTime = DateTime.Now;
		float fractionOfDay = currentTime.Hour / 24f + currentTime.Minute / 1440f;
		return fractionOfDay;
	}

	public float GetTimeOfDay()
	{
		DateTime currentTime = DateTime.Now;
		float fractionOfDay = currentTime.Hour / 24f + currentTime.Minute / 1440f;
		return fractionOfDay;
	}
}
