using System.Collections.Generic;
using UnityEngine;
using SaveLoad.Data;
using Terrain.Generation;

namespace SaveLoad.Management
{
	public class SavableEntitySpawner : MonoBehaviour
	{
		[SerializeField] private SavableEntityIndex m_entityIndex;

		private void OnEnable()
		{
			SaveManager.ChunkEntitiesLoaded += SpawnEntitiesForChunk;
		}

		private void OnDisable()
		{
			SaveManager.ChunkEntitiesLoaded -= SpawnEntitiesForChunk;
		}

		private void SpawnEntitiesForChunk(TerrainChunk chunk, List<EntitySaveData> savedEntities)
		{
			foreach (EntitySaveData entityData in savedEntities)
			{
				TrySpawnSavableEntity(chunk, entityData);
			}
		}

		private GameObject GetPrefabById(int prefabId)
		{
			foreach (var data in m_entityIndex.SavableEntityPrefabData)
			{
				if (data.PrefabID == prefabId)
				{
					return data.EntityPrefab;
				}
			}
			return null;
		}

		private void TrySpawnSavableEntity(TerrainChunk chunk, EntitySaveData entityData)
		{
			GameObject prefabToSpawn = GetPrefabById(entityData.PrefabId);

			if (prefabToSpawn == null)
			{
				Debug.LogWarning($"Could not find prefab with ID {entityData.PrefabId} in index!");
				return;
			}

			GameObject spawnedEntity = Instantiate(prefabToSpawn);

			// Restore the savable entitiy component
			if (spawnedEntity.TryGetComponent(out SaveableEntity saveableEntity))
			{
				saveableEntity.RestoreFromSaveData(entityData);
				chunk.RegisterEntity(spawnedEntity);
			}
		}

		private void TrySpawnPersistentSavableEntity(TerrainChunk chunk, EntitySaveData entityData)
		{
			SaveableEntity[] allEntities = FindObjectsByType<SaveableEntity>(sortMode: FindObjectsSortMode.InstanceID);

			foreach (SaveableEntity entity in allEntities)
			{
				// Look for the entity that matches the saved GUID
				if (entity.GetGUID() == entityData.GUID)
				{
					// Restore data
					entity.RestoreFromSaveData(entityData);
					chunk.RegisterEntity(entity.gameObject);
					return;
				}
			}

			Debug.LogWarning($"Could not find persistent entity with GUID {entityData.GUID} in the scene. Was it destroyed?");
		}
	}
}