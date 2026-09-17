using Entities.Savable;
using Player.Core;
using SaveLoad.Core;
using SaveLoad.Data;
using System;
using UnityEngine;

public class PlayerSaveComponent : MonoBehaviour, ISavableEntity
{
	private const string c_GUID = "Player";

	private PlayerController m_playerController;

	// ISavableEntity properties
	public bool SavedByChunks => false;

	// Events
	public event Action DataRestored;
	public event Action<Vector3, Quaternion> TransformRestored;

	private void Awake()
	{
		m_playerController = GetComponent<PlayerController>();

		if (m_playerController != null)
			m_playerController.enabled = false;
	}

	private void OnEnable()
	{
		SaveEvents.PlayerEntityDataRequested += ProvidePlayerData;
		SaveEvents.GameLoaded += ApplyLoadedData;
	}

	private void OnDestroy()
	{
		SaveEvents.PlayerEntityDataRequested -= ProvidePlayerData;
		SaveEvents.GameLoaded -= ApplyLoadedData;
	}

	private SerializablePlayerData ProvidePlayerData()
	{
		return new SerializablePlayerData(DateTime.Now, GenerateSaveData());
	}

	public SerializableEntityData GenerateSaveData()
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

		return data;
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
		Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);
		Quaternion rotation = Quaternion.Euler(data.RotX, data.RotY, data.RotZ);
		TransformRestored?.Invoke(position, rotation);

		if (m_playerController != null)
		{
			m_playerController.enabled = true;
			m_playerController.Teleport(position);
			transform.rotation = rotation;
		}

		// Restore component data
		ISaveableComponent[] saveableComponents = GetComponentsInChildren<ISaveableComponent>();
		foreach (var component in saveableComponents)
		{
			string compId = component.GetComponentId();

			if (data.ComponentData.TryGetValue(compId, out object savedComponentData))
				component.RestoreComponentData(savedComponentData);
		}
		DataRestored?.Invoke();

		Debug.Log("Player state and components restored successfully!");
	}
}