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
}
