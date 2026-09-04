using GenericIndex;
using System;
using System.Collections.Generic;
using UnityEngine;
using Entities.Core;
using WorldManagement.Core;
using SaveLoad.Core;
using SaveLoad.Data;

namespace Entities.Savable
{
	[RequireComponent(typeof(Entities.Core.Entity))]
	public class SaveableEntity : MonoBehaviour, ISavableEntity
	{
		[SerializeField] private SavableEntityPrefabData m_savablePrefabData;

		[SerializeField] private string m_guid = "";
		public string GetGUID() => m_guid;

		[field: SerializeField, Tooltip("Should be true when an object is not spawned at run-time.")] 
		public bool IsManuallyAuthored { get; private set; } = false;

		// Events
		public event Action DataRestored;
		public event Action<Vector3, Quaternion> TransformRestored;

		// System
		private Entity m_entity;
		private Vector2Int m_chunkXZ = default;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(m_guid) && gameObject.scene.IsValid())
			{
				m_guid = System.Guid.NewGuid().ToString();
				UnityEditor.EditorUtility.SetDirty(this);
				UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
			}
		}

		[ContextMenu("Force Generate GUID")]
		private void ForceGenerateGUID()
		{
			m_guid = System.Guid.NewGuid().ToString();
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
		}
#endif

		private void Awake()
		{
			m_entity = GetComponent<Entity>();
			m_entity.EntityPositionChanged += OnEntityMoved;

			if (string.IsNullOrEmpty(m_guid) && gameObject.scene.IsValid())
			{
				m_guid = System.Guid.NewGuid().ToString();
			}
		}

		private void OnEnable()
		{
			RegisterToClosestChunk();
		}

		private void OnDestroy()
		{
			if (m_entity != null)
				m_entity.EntityPositionChanged -= OnEntityMoved;

			UnregisterFromCurrentChunk();
		}

		private void OnEntityMoved()
		{
			RegisterToClosestChunk();
		}

		private void RegisterToClosestChunk()
		{
			Vector2Int entityChunkXZ = CoordinateUtility.WorldToChunkXZ(transform.position);
			if (entityChunkXZ != m_chunkXZ || m_chunkXZ == default)
			{
				// Unregister this entity from its previous chunk
				if (m_chunkXZ != default)
				{
					TerrainChunk previousTerrainChunk = WorldManager.GetChunkData(m_chunkXZ);
					if (previousTerrainChunk != null)
						previousTerrainChunk.UnregisterEntity(gameObject);
				}

				// Register this entity to the chunk it overlaps with
				m_chunkXZ = entityChunkXZ;
				TerrainChunk terrainChunk = WorldManager.GetChunkData(m_chunkXZ);

				if (terrainChunk != null)
					terrainChunk.RegisterEntity(gameObject);

				if (WorldManager.s_ActiveChunks.TryGetValue(entityChunkXZ, out var activeChunkTuple) && activeChunkTuple.gameObject != null)
				{
					transform.parent = activeChunkTuple.gameObject.transform;
				}
			}
		}

		private void UnregisterFromCurrentChunk()
		{
			// Unregister this entity from its previous chunk
			TerrainChunk previousTerrainChunk = WorldManager.GetChunkData(m_chunkXZ);

			if (previousTerrainChunk != null)
				previousTerrainChunk.UnregisterEntity(gameObject);

			m_chunkXZ = default;
		}

		/// <summary>
		/// Gathers data from all ISaveableComponent scripts on this GameObject
		/// </summary>
		public SerializableEntityData GenerateSaveData()
		{
			SerializableEntityData data = null;

			// Get prefab id from index
			int prefabID = GetPrefabID();
			if (prefabID == -1)
				return null;

			data = new SerializableEntityData
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

			ISaveableComponent[] saveableComponents = GetComponentsInChildren<ISaveableComponent>();
			foreach (var component in saveableComponents)
			{
				data.ComponentData[component.GetComponentId()] = component.GenerateComponentData();
			}

			return data;
		}

		/// <summary>
		/// Pushes the loaded data back into the individual components
		/// </summary>
		public void RestoreFromSaveData(SerializableEntityData data)
		{
			this.m_guid = data.GUID;

			// Restore the entities transform
			Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);
			Quaternion rotation = Quaternion.Euler(data.RotX, data.RotY, data.RotZ);
			if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var navAgent))
			{
				navAgent.Warp(position);
				transform.rotation = rotation;
			}
			else
			{
				transform.position = position;
				transform.rotation = rotation;
			}

			TransformRestored?.Invoke(position, rotation);

			// Restore component data
			ISaveableComponent[] saveableComponents = GetComponentsInChildren<ISaveableComponent>();
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