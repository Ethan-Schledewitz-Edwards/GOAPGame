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
			WorldManager.ChunkSpawnedEntities += HandleChunkLoadedEntities;
		}

		private void OnDestroy()
		{
			WorldManager.ChunkSpawnedEntities -= HandleChunkLoadedEntities;
		}

		private void HandleChunkLoadedEntities(TerrainChunk chunk, List<SerializableEntityData> savedEntities)
		{
			bool isAuthoredWorld = FindAnyObjectByType<TerrainChunkManager>().BuilderMethod == TerrainChunkManager.EChunkBuilderMethod.Authored;

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

		/// <summary>
		/// Attempts to locate and restore a persistent savable entity in the specified terrain chunk using the provided
		/// entity data, or spawns a new instance if not found.
		/// </summary>
		/// <param name="chunk">The terrain chunk in which to spawn or restore the entity.</param>
		/// <param name="entityData">The serialized data containing information about the entity to be spawned or restored.</param>
		private void TrySpawnPersistentSavableEntity(TerrainChunk chunk, SerializableEntityData entityData)
		{
			SaveableEntity[] allEntities = FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include);
			foreach (SaveableEntity saveableEntity in allEntities)
			{
				if (saveableEntity.GetGUID() == entityData.GUID)
				{
					if (WorldManager.s_ActiveChunks.TryGetValue(chunk.ChunkXZ, out var activeChunkTuple))
						saveableEntity.transform.parent = activeChunkTuple.gameObject.transform;

					saveableEntity.RestoreFromSaveData(entityData);
					chunk.RegisterEntity(saveableEntity.gameObject);

					if (!saveableEntity.gameObject.activeSelf)
						saveableEntity.gameObject.SetActive(true);

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