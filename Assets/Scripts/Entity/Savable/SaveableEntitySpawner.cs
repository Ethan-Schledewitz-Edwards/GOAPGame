using SaveLoad.Core;
using SaveLoad.Data;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.Core;

namespace Entities.Savable
{
	public class SavableEntitySpawner : MonoBehaviour
	{
		[SerializeField] private SavableEntityIndex m_entityIndex;

		private void OnEnable()
		{
			WorldSaveHandler.ChunkReadyForEntities += SpawnEntitiesForChunk;
		}

		private void OnDisable()
		{
			WorldSaveHandler.ChunkReadyForEntities -= SpawnEntitiesForChunk;
		}

		private void SpawnEntitiesForChunk(TerrainChunk chunk, List<SerializableEntityData> savedEntities)
		{
			bool isAuthoredWorld = FindFirstObjectByType<TerrainChunkManager>().BuilderMethod == TerrainChunkManager.EChunkBuilderMethod.Authored;

			foreach (SerializableEntityData entityData in savedEntities)
			{
				if (isAuthoredWorld)
				{
					TrySpawnPersistentSavableEntity(chunk, entityData);
				}
				else
				{
					TrySpawnSavableEntity(chunk, entityData);
				}
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

		private void TrySpawnSavableEntity(TerrainChunk chunk, SerializableEntityData entityData)
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

		private void TrySpawnPersistentSavableEntity(TerrainChunk chunk, SerializableEntityData entityData)
		{
			SaveableEntity[] allEntities = FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

			foreach (SaveableEntity entity in allEntities)
			{
				if (entity.GetGUID() == entityData.GUID)
				{
					entity.RestoreFromSaveData(entityData);
					chunk.RegisterEntity(entity.gameObject);
					return;
				}
			}

			// Try to spawn normally
			TrySpawnSavableEntity(chunk, entityData);
		}
	}
}