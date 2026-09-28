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
			if (savedEntities == null)
				return;

			bool isAuthoredWorld = TerrainChunkManager.s_BuilderMethod == 
				TerrainChunkManager.EChunkBuilderMethod.Authored;

			foreach (SerializableEntityData entityData in savedEntities)
			{
				if (entityData == null)
					continue;

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
			// Loading the same GUID twice must never create two entities.
			if (SavableEntity.TryGetByGuid(entityData.GUID, out SavableEntity existing))
			{
				Debug.LogWarning($"[SavableEntitySpawner] Entity GUID " +
					$"'{entityData.GUID}' already exists as " +
					$"'{existing.name}'. Restoring existing instance.");

				existing.RestoreFromSaveData(entityData);
				chunk.RegisterEntity(existing.gameObject);
				return;
			}

			SavableEntityPrefabData prefabData = FindPrefabData(entityData);
			if (prefabData == null)
			{
				Debug.LogError($"[SavableEntitySpawner] Could not find a prefab " +
					$"for entity GUID '{entityData.GUID}', " +
					$"PrefabKey '{entityData.PrefabKey}'.");

				return;
			}

			GameObject prefab = prefabData.EntityPrefab;
			if (prefab == null)
			{
				Debug.LogError($"[SavableEntitySpawner] Prefab data " +
					$"'{prefabData.name}' has no EntityPrefab.");

				return;
			}

			GameObject spawnedEntity = Instantiate(prefab);
			if (!spawnedEntity.TryGetComponent(out SavableEntity saveableEntity))
			{
				Debug.LogError($"[SavableEntitySpawner] Prefab '{prefab.name}' " +
					$"does not contain a SaveableEntity component.", spawnedEntity);

				Destroy(spawnedEntity);
				return;
			}

			if (!saveableEntity.RestoreFromSaveData(entityData))
			{
				Destroy(spawnedEntity);
				return;
			}

			chunk.RegisterEntity(spawnedEntity);
		}

		private void TrySpawnPersistentSavableEntity(TerrainChunk chunk, SerializableEntityData entityData)
		{
			if (SavableEntity.TryGetByGuid(entityData.GUID, out SavableEntity existing))
			{
				existing.RestoreFromSaveData(entityData);
				chunk.RegisterEntity(existing.gameObject);

				if (!existing.gameObject.activeSelf)
					existing.gameObject.SetActive(true);

				return;
			}

			// The entity either has the wrong GUID or is not authored savable.
			// Fallback by spawning the entity normally.
			TrySpawnSavableEntity(chunk, entityData);
		}

		private SavableEntityPrefabData FindPrefabData(SerializableEntityData entityData)
		{
			if (m_entityIndex == null)
				return null;

			if (!string.IsNullOrEmpty(entityData.PrefabKey))
			{
				SavableEntityPrefabData data = m_entityIndex.GetIndexedAsset(entityData.PrefabKey);

				if (data != null)
					return data;
			}

			return null;
		}
	}
}