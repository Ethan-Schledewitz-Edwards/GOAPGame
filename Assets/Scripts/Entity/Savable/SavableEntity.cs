using Entities.Core;
using GenericIndex;
using SaveLoad.Core;
using SaveLoad.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.AuthoredTiles;
using WorldManagement.Core;

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

		// ISavableEntity properties
		public bool SavedByChunks => true;

		// Events
		public event Action DataRestored;
		public event Action<Vector3, Quaternion> TransformRestored;

		// System
		private Entity m_entity;
		private Vector2Int m_chunkXZ = default;
		private LayerMask m_collisionLayerMask;

		private bool m_useGravityByDefault;
		private bool m_isKinematicByDefault;
		private bool m_isPhysicsEnabled;
		private bool m_isInitializedFromSave;

		private Collider m_collider;
		private Rigidbody m_rigidbody;

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
			m_entity.EntityChunkChanged += RegisterToClosestChunk;

			m_collider = GetComponent<Collider>();
			m_rigidbody = GetComponent<Rigidbody>();
			if (m_rigidbody != null)
			{
				m_useGravityByDefault = m_rigidbody.useGravity;
				m_isKinematicByDefault = m_rigidbody.isKinematic;
			}

			DisablePhysicsAndCollision();

			if (string.IsNullOrEmpty(m_guid) && gameObject.scene.IsValid())
			{
				m_guid = System.Guid.NewGuid().ToString();
			}
		}

		private void Start()
		{
			m_collisionLayerMask = LayerMask.GetMask("Default", "Environment", "Interaction");

			if (!m_isInitializedFromSave && !IsManuallyAuthored)
				InitializeRuntimeEntity();
			else if (IsManuallyAuthored)
				EnablePhysicsAndCollision();

			Vector2Int entityChunkXZ = CoordinateUtility.WorldToChunkXZ(transform.position);
			RegisterToClosestChunk(entityChunkXZ);
		}

		private void OnDestroy()
		{
			if (m_entity != null)
				m_entity.EntityChunkChanged -= RegisterToClosestChunk;

			UnregisterFromCurrentChunk();
			StopAllCoroutines();
		}

		private void InitializeRuntimeEntity()
		{
			if (string.IsNullOrEmpty(m_guid) || m_guid == System.Guid.Empty.ToString())
			{
				m_guid = System.Guid.NewGuid().ToString();
			}

			EnablePhysicsAndCollision();
		}

		private void RegisterToClosestChunk(Vector2Int chunkXZ)
		{
			if (chunkXZ != m_chunkXZ || m_chunkXZ == default)
			{
				// Unregister this entity from its previous chunk
				if (m_chunkXZ != default)
				{
					TerrainChunk previousTerrainChunk = WorldManager.GetChunkData(m_chunkXZ);
					if (previousTerrainChunk != null)
						previousTerrainChunk.UnregisterEntity(gameObject);
				}

				// Register this entity to the chunk it overlaps with
				m_chunkXZ = chunkXZ;
				TerrainChunk terrainChunk = WorldManager.GetChunkData(m_chunkXZ);

				if (terrainChunk != null)
					terrainChunk.RegisterEntity(gameObject);

				if (!IsManuallyAuthored)
				{
					if (WorldManager.s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunkTuple) && activeChunkTuple.gameObject != null)
					{
						transform.parent = activeChunkTuple.gameObject.transform;
					}
				}
				else
				{
					if(AuthoredTileLoader.s_AuthoredChunks.TryGetValue(m_chunkXZ, out GameObject chunkObject))
					{
						transform.parent = chunkObject.transform;
					}
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

		private void EnablePhysicsAndCollision()
		{
			if (m_collider != null)
				m_collider.enabled = true;

			if (m_rigidbody != null)
			{
				m_rigidbody.useGravity = m_useGravityByDefault;
				m_rigidbody.isKinematic = m_isKinematicByDefault;

				m_isPhysicsEnabled = true;
			}
		}

		private void DisablePhysicsAndCollision()
		{
			if (m_collider != null)
				m_collider.enabled = false;

			if (m_rigidbody != null)
			{
				m_rigidbody.useGravity = false;
				m_rigidbody.isKinematic = true;
			}

			m_isPhysicsEnabled = false;
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
			m_isInitializedFromSave = true;
			this.m_guid = data.GUID;

			// Restore the entities transform
			Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);
			Quaternion rotation = Quaternion.Euler(data.RotX, data.RotY, data.RotZ);
			transform.position = position;
			transform.rotation = rotation;
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

			EnablePhysicsAndCollision();
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