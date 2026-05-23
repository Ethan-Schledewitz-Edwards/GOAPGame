using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	public GameObject PlayerObject { get; private set; }

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else Destroy(Instance);
	}

	public void SetPlayer(GameObject playerObject)
	{
		PlayerObject = playerObject;
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
