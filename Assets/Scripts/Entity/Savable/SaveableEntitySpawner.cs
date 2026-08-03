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
			WorldManager.ChunkSpawnedEntities += SpawnEntitiesForChunk;
		}

		private void OnDisable()
		{
			WorldManager.ChunkSpawnedEntities -= SpawnEntitiesForChunk;
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
			foreach (SavableEntityPrefabData data in m_entityIndex.SavableEntityPrefabData)
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
			GameObject prefabToSpawn = m_entityIndex.GetIndexedAsset(entityData.PrefabId).EntityPrefab;

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
					if (WorldManager.s_ActiveChunks.TryGetValue(chunk.ChunkXZ, out var activeChunkTuple))
						entity.transform.parent = activeChunkTuple.gameObject.transform;

					entity.RestoreFromSaveData(entityData);
					chunk.RegisterEntity(entity.gameObject);

					if (!entity.gameObject.activeSelf)
						entity.gameObject.SetActive(true);

					return;
				}
			}

			// Try to spawn normally
			Debug.LogWarning($"[SaveableEntitySpawner] Could not find a persistent SavableEntity with GUID {entityData.GUID} in the scene. " +
				$"Spawning duplicate from Prefab ID {entityData.PrefabId}. Ensure Editor GUIDs are serialized if this entity is persistent!");
			TrySpawnSavableEntity(chunk, entityData);
		}
	}
}