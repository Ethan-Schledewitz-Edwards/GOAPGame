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
	public Vector2Int ChunkXZ => default;
	public bool IsRegisteredToChunk => false;

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
		Vector3 playerPosition = transform.position;
		Quaternion playerRotation = m_playerController.Rotation;

		SerializableEntityData data = new SerializableEntityData
		{
			GUID = c_GUID,
			PrefabKey = "",
			PosX = playerPosition.x,
			PosY = playerPosition.y,
			PosZ = playerPosition.z,
			RotX = playerRotation.x,
			RotY = playerRotation.y,
			RotZ = playerRotation.z,
			RotW = playerRotation.w
		};

		ISaveableComponent[] saveableComponents = 
			GetComponentsInChildren<ISaveableComponent>();

		foreach (var component in saveableComponents)
		{
			object rawData = component.GenerateComponentData();
			if (rawData is string dataString)
			{
				data.ComponentData.Add(new ComponentSaveData
				{
					K = component.GetComponentId(),
					V = dataString
				});
			}
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
		Vector3 position = 
			new Vector3(data.PosX, data.PosY, data.PosZ);

		Quaternion rotation = 
			new Quaternion(data.RotX, data.RotY, data.RotZ, data.RotW);

		Debug.Log("Player transform restored successfully!");
		TransformRestored?.Invoke(position, rotation);

		if (m_playerController != null)
		{
			m_playerController.enabled = true;
			m_playerController.Teleport(position);
			m_playerController.Rotate(rotation);
		}

		// Restore component data
		ISaveableComponent[] saveableComponents = 
			GetComponentsInChildren<ISaveableComponent>();

		foreach (var component in saveableComponents)
		{
			string compId = component.GetComponentId();
			ComponentSaveData entry = data.ComponentData.Find(x => x.K == compId);
			if (entry != null)
			{
				component.RestoreComponentData(entry.V);
			}
		}

		Debug.Log("Player state and components restored successfully!");
		DataRestored?.Invoke();
	}
}