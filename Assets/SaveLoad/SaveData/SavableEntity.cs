using GenericIndex;
using System;
using System.Collections.Generic;
using UnityEngine;
using Terrain.Generation;
using Terrain.WorldProperties;

namespace SaveLoad.Data
{
	[RequireComponent(typeof(Entity))]
	public class SaveableEntity : MonoBehaviour
	{
		[SerializeField] private SavableEntityPrefabData m_savablePrefabData;

		[SerializeField] private string m_guid = "";
		public string GetGUID() => m_guid;

		// Events
		public event Action DataRestored;
		public event Action<Vector3, Quaternion> TransformRestored;

		// System
		private Entity m_entity;

		private bool m_isMoveable = false;
		private Vector2Int m_chunkXZ;

		private void Awake()
		{
			m_entity = GetComponent<Entity>();
			m_entity.EntityPositionChanged += OnEntityMoved;

			if (string.IsNullOrEmpty(m_guid) && gameObject.scene.IsValid())
			{
				m_guid = System.Guid.NewGuid().ToString();
			}
		}

		private void OnDestroy()
		{
			if (m_entity != null)
				m_entity.EntityPositionChanged -= OnEntityMoved;

			UnregisterFromClosestChunk();
		}

		public void InitializeSavableEntity()
		{
			RegisterToClosestChunk();
		}

		private void OnEntityMoved()
		{
			RegisterToClosestChunk();
		}

		private void RegisterToClosestChunk()
		{
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;
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
			Vector3Int chunkSize = WorldPropertyUtility.s_ChunkSize;
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

		/// <summary>
		/// Gathers data from all ISaveableComponent scripts on this GameObject
		/// </summary>
		public EntitySaveData GenerateSaveData()
		{
			EntitySaveData data = null;

			// Get prefab id from index
			int prefabID = GetPrefabID();
			if (prefabID == -1)
				return null;

			data = new EntitySaveData
			{
				GUID = this.m_guid,
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
			this.m_guid = data.GUID;

			// Restore the entities transform
			Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);
			transform.position = position;
			Quaternion rotation = Quaternion.Euler(data.RotX, data.RotY, data.RotZ);
			transform.rotation = rotation;
			TransformRestored?.Invoke(position, rotation);

			// Restore component data
			ISaveable[] saveableComponents = GetComponentsInChildren<ISaveable>();
			foreach (var component in saveableComponents)
			{
				string compId = component.GetComponentId();

				if (data.ComponentData.TryGetValue(compId, out object savedComponentData))
					component.RestoreComponentData(savedComponentData);
			}
			DataRestored?.Invoke();
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