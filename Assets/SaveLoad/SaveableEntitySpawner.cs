using System.Collections.Generic;
using UnityEngine;
using SaveLoad.Data;


namespace SaveLoad.Management
{
	public class SavableEntitySpawner : MonoBehaviour
	{
		[SerializeField] private SavableEntityIndex m_entityIndex;

		private void OnEnable()
		{
			SaveManager.OnChunkEntitiesLoaded += SpawnEntitiesForChunk;
		}

		private void OnDisable()
		{
			SaveManager.OnChunkEntitiesLoaded -= SpawnEntitiesForChunk;
		}

		private void SpawnEntitiesForChunk(TerrainChunk chunk, List<EntitySaveData> savedEntities)
		{
			foreach (EntitySaveData entityData in savedEntities)
			{
				GameObject prefabToSpawn = GetPrefabById(entityData.PrefabId);

				if (prefabToSpawn == null)
				{
					Debug.LogWarning($"Could not find prefab with ID {entityData.PrefabId} in index!");
					continue;
				}

				Vector3 spawnPos = new Vector3(entityData.PosX, entityData.PosY, entityData.PosZ);
				Quaternion spawnRot = Quaternion.Euler(entityData.RotX, entityData.RotY, entityData.RotZ);
				GameObject spawnedEntity = Instantiate(prefabToSpawn, spawnPos, spawnRot);

				// Restore the savable entitiy component
				if (spawnedEntity.TryGetComponent(out SaveableEntity saveableEntity))
				{
					saveableEntity.RestoreFromSaveData(entityData);
					chunk.RegisterEntity(spawnedEntity);
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
	}
}