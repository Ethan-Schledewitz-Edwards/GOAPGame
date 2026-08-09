using Entities.Savable;
using Player.Core;
using SaveLoad.Core;
using SaveLoad.Data;
using System;
using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour
{
	private const string c_GUID = "Player";

	private PlayerController m_playerController;

	private void Awake()
	{
		m_playerController = GetComponent<PlayerController>();

		if (m_playerController != null)
			m_playerController.enabled = false;
	}

	private void OnEnable()
	{
		SaveEvents.PlayerDataRequested += ProvidePlayerData;
		SaveEvents.GameLoaded += ApplyLoadedData;
	}

	private void OnDestroy()
	{
		SaveEvents.PlayerDataRequested -= ProvidePlayerData;
		SaveEvents.GameLoaded -= ApplyLoadedData;
	}

	private SerializablePlayerData ProvidePlayerData()
	{
		Vector3 playerRotation = transform.eulerAngles;

		SerializableEntityData data = new SerializableEntityData
		{
			GUID = c_GUID,
			PrefabId = -1,
			PosX = transform.position.x,
			PosY = transform.position.y,
			PosZ = transform.position.z,
			RotX = playerRotation.x,
			RotY = playerRotation.y,
			RotZ = playerRotation.z
		};

		ISaveableComponent[] saveableComponents = GetComponentsInChildren<ISaveableComponent>();
		foreach (var component in saveableComponents)
		{
			data.ComponentData[component.GetComponentId()] = component.GenerateComponentData();
		}

		return new SerializablePlayerData(DateTime.Now, data);
	}

	private void ApplyLoadedData(SerializablePlayerData saveFile)
	{
		// Handle new save
		if (saveFile == null || saveFile.PlayerData == null)
		{
			if (m_playerController != null)
				m_playerController.enabled = true;
			return;
		}

		SerializableEntityData data = saveFile.PlayerData;

		// Restore Position and Rotation
		Vector3 spawnPosition = new Vector3(data.PosX, data.PosY, data.PosZ);
		m_playerController.Teleport(spawnPosition);
		transform.rotation = Quaternion.Euler(data.RotX, data.RotY, data.RotZ);

		// Restore component data
		ISaveableComponent[] saveableComponents = GetComponentsInChildren<ISaveableComponent>();
		foreach (var component in saveableComponents)
		{
			string compId = component.GetComponentId();

			if (data.ComponentData.TryGetValue(compId, out object savedComponentData))
				component.RestoreComponentData(savedComponentData);
		}

		if (m_playerController != null)
			m_playerController.enabled = true;

		Debug.Log("Player state and components restored successfully!");
	}
}