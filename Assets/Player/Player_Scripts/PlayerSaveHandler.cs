using SaveLoad.Data;
using SaveLoad.Management;
using UnityEngine;

public class PlayerSaveHandler : MonoBehaviour
{
	private const string c_GUID = "Player";

	private PlayerController m_playerController;

	private void Awake()
	{
		m_playerController = GetComponent<PlayerController>();
		
		if(m_playerController != null )
			m_playerController.enabled = false;
	}

	private void OnEnable()
	{
		SaveManager.RequestPlayerData += ProvidePlayerData;
		SaveManager.GameLoaded += ApplyLoadedData;
	}

	private void OnDestroy()
	{
		SaveManager.RequestPlayerData -= ProvidePlayerData;
		SaveManager.GameLoaded -= ApplyLoadedData;
	}

	private EntitySaveData ProvidePlayerData()
	{
		EntitySaveData data = new EntitySaveData
		{
			GUID = c_GUID,
			PrefabId = -1,
			PosX = transform.position.x,
			PosY = transform.position.y,
			PosZ = transform.position.z,
			RotX = transform.rotation.x,
			RotY = transform.rotation.y,
			RotZ = transform.rotation.z
		};

		ISaveable[] saveableComponents = GetComponentsInChildren<ISaveable>();
		foreach (var component in saveableComponents)
		{
			data.ComponentData[component.GetComponentId()] = component.GenerateComponentData();
		}

		return data;
	}

	private void ApplyLoadedData(SaveManager.SaveData saveFile)
	{
		EntitySaveData data = saveFile.PlayerData;

		if (data == null)
		{
			if (m_playerController != null)
				m_playerController.enabled = true;

			return;
		}

		// Restore Position and Rotation
		Vector3 spawnPosition = new Vector3(data.PosX, data.PosY, data.PosZ);
		m_playerController.Teleport(spawnPosition);
		transform.rotation = Quaternion.Euler(data.RotX, data.RotY, data.RotZ);

		// Restore component data
		ISaveable[] saveableComponents = GetComponentsInChildren<ISaveable>();
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