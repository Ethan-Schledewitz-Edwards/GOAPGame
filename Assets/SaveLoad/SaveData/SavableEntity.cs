using GenericIndex;
using System.Collections.Generic;
using UnityEngine;

namespace SaveLoad.Data
{
	public class SaveableEntity : MonoBehaviour
	{
		[SerializeField] private SavableEntityPrefabData m_savablePrefabData;

		private string m_guid = "";
		public string GetGuid() => m_guid;
		public void SetGuid(string guid) => m_guid = guid;

		private Vector2Int m_chunkXZ;

		private void Awake()
		{
			if (string.IsNullOrEmpty(m_guid) && gameObject.scene.IsValid())
			{
				m_guid = System.Guid.NewGuid().ToString();
			}
		}

		private void OnDestroy()
		{
			UnregisterFromClosestChunk();
		}

		public void InitializeSavableEntity()
		{
			RegisterToClosestChunk();
		}

		/// <summary>
		/// Gathers data from all ISaveableComponent scripts on this GameObject
		/// </summary>
		public EntitySaveData GenerateSaveData()
		{
			int prefabID = GetPrefabID();
			if(prefabID == -1)
				return null;

			EntitySaveData data = new EntitySaveData
			{
				Guid = this.m_guid,
				PrefabId = prefabID,
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

		/// <summary>
		/// Pushes the loaded data back into the individual components
		/// </summary>
		public void RestoreFromSaveData(EntitySaveData data)
		{
			this.m_guid = data.Guid;

			ISaveable[] saveableComponents = GetComponentsInChildren<ISaveable>();

			foreach (var component in saveableComponents)
			{
				string compId = component.GetComponentId();

				if (data.ComponentData.TryGetValue(compId, out object savedComponentData))
					component.RestoreComponentData(savedComponentData);
			}
		}

		private void Update()
		{
			RegisterToClosestChunk();
		}

		private void RegisterToClosestChunk()
		{
			Vector3Int chunkSize = WorldBuilder.s_ChunkSize;
			Vector2Int currentChunkXZ = new Vector2Int
				(
					Mathf.FloorToInt(transform.position.x / chunkSize.x),
					Mathf.FloorToInt(transform.position.z / chunkSize.z)
				);

			if (currentChunkXZ != m_chunkXZ)
			{
				// Unregister this entity from its previous chunk
				TerrainChunk previousTerrainChunk = WorldBuilder.GetChunkData(m_chunkXZ);
				previousTerrainChunk?.UnregisterEntity(gameObject);

				// Register this entity to its new chunk
				m_chunkXZ = currentChunkXZ;
				TerrainChunk terrainChunk = WorldBuilder.GetChunkData(m_chunkXZ);
				terrainChunk?.RegisterEntity(gameObject);
			}
		}

		public void UnregisterFromClosestChunk()
		{
			Vector3Int chunkSize = WorldBuilder.s_ChunkSize;
			Vector2Int currentChunkXZ = new Vector2Int
				(
					Mathf.FloorToInt(transform.position.x / chunkSize.x),
					Mathf.FloorToInt(transform.position.z / chunkSize.z)
				);

			if (currentChunkXZ != m_chunkXZ)
			{
				// Unregister this entity from its previous chunk
				TerrainChunk previousTerrainChunk = WorldBuilder.GetChunkData(m_chunkXZ);
				previousTerrainChunk?.UnregisterEntity(gameObject);

				// Register this entity to its new chunk
				m_chunkXZ = currentChunkXZ;
				TerrainChunk terrainChunk = WorldBuilder.GetChunkData(m_chunkXZ);
				terrainChunk?.UnregisterEntity(gameObject);
			}
		}

		private int GetPrefabID()
		{
			if(m_savablePrefabData == null)
			{
				Debug.LogWarning($"No {typeof(SavableEntityPrefabData)} was found on {gameObject.name}. Savable entities must have data assigned to be saved");
				return -1;
			}

			return m_savablePrefabData.PrefabID;
		}
	}
}