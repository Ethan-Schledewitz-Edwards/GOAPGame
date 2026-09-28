using UnityEngine;
using UnityEditor;
using System.IO;

namespace Entities.Savable
{
#if UNITY_EDITOR

	using UnityEditor.SceneManagement;

	/// <summary>
	/// AI generated editor tooling because IDGAF about learning how to make editor tools lmao... 
	/// I proof read it and modified it at least.
	/// Ethan
	/// </summary>
	[CustomEditor(typeof(SavableEntity))]
	public class SaveableEntityEditor : UnityEditor.Editor
	{
		private const string c_indexAssetPath =
			"Assets/Scripts/Entity/Savable/SavableEntityPrefabDataIndex.asset";

		private const string c_prefabDataSaveFolderPath =
			"Assets/Scripts/Entity/Savable/SavableEntityPrefabData/";

		private SerializedProperty m_isManuallyAuthoredProp;
		private SerializedProperty m_savablePrefabDataProp;
		private SerializedProperty m_guidProp;

		private void OnEnable()
		{
			m_isManuallyAuthoredProp = serializedObject.FindProperty("<IsManuallyAuthored>k__BackingField");
			m_savablePrefabDataProp = serializedObject.FindProperty("m_savablePrefabData");
			m_guidProp = serializedObject.FindProperty("m_guid");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUI.enabled = false;
			EditorGUILayout.PropertyField(m_guidProp);
			GUI.enabled = true;

			EditorGUILayout.PropertyField(m_isManuallyAuthoredProp);
			EditorGUILayout.PropertyField(m_savablePrefabDataProp);

			bool isPrefabStageOrAsset = PrefabStageUtility.GetCurrentPrefabStage() != null ||
				EditorUtility.IsPersistent(target);

			// Correctly check if GUID string is empty
			if (!isPrefabStageOrAsset)
			{
				GUI.enabled = false;
				EditorGUILayout.PropertyField(m_guidProp);
				GUI.enabled = true;

				if (string.IsNullOrEmpty(m_guidProp.stringValue))
				{
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox("This entity is missing a GUID. " +
						"Generate one before saving.", MessageType.Error);

					if (GUILayout.Button("Generate GUID", GUILayout.Height(30)))
					{
						m_guidProp.stringValue = System.Guid.NewGuid().ToString();
						serializedObject.ApplyModifiedProperties();

						EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					}
				}
			}

			if (m_savablePrefabDataProp.objectReferenceValue == null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("This entity is missing its Prefab Data. " +
					"Savable entities must have data assigned to be saved.", MessageType.Warning);

				if (GUILayout.Button("Create & Assign Prefab Data", GUILayout.Height(30)))
				{
					CreateAndAssignPrefabData();
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void CreateAndAssignPrefabData()
		{
			SavableEntity entity = (SavableEntity)target;
			GameObject prefabAsset = null;

			PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null && prefabStage.IsPartOfPrefabContents(entity.gameObject))
			{
				prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
			}
			else
			{
				prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(entity.gameObject);
			}

			if (prefabAsset == null)
			{
				Debug.LogError("Cannot create Prefab Data: The selected object is neither a " +
					"Prefab instance in the scene nor being edited in Prefab Mode.");
				return;
			}

			if (!Directory.Exists(c_prefabDataSaveFolderPath))
			{
				Directory.CreateDirectory(c_prefabDataSaveFolderPath);
			}

			string assetName = $"{prefabAsset.name}_SavableEntityPrefabData.asset";
			string path = AssetDatabase.GenerateUniqueAssetPath(c_prefabDataSaveFolderPath + assetName);

			SavableEntityPrefabData newData = ScriptableObject.CreateInstance<SavableEntityPrefabData>();

			// Assign values via SerializedObject first
			SerializedObject newDataSO = new SerializedObject(newData);
			SerializedProperty entityPrefabProp = newDataSO.FindProperty("<EntityPrefab>k__BackingField");

			if (entityPrefabProp != null)
			{
				entityPrefabProp.objectReferenceValue = prefabAsset;
			}

			newDataSO.ApplyModifiedProperties();

			AssetDatabase.CreateAsset(newData, path);
			newData.SetID(assetName);

			// Assign to property
			m_savablePrefabDataProp.objectReferenceValue = newData;
			serializedObject.ApplyModifiedProperties();

			// Highlight the asset in the Project window
			EditorGUIUtility.PingObject(newData);

			AddToIndex();
		}

		private void AddToIndex()
		{
			SavableEntityIndex indexAsset = 
				AssetDatabase.LoadAssetAtPath<SavableEntityIndex>(c_indexAssetPath);

			if (indexAsset == null)
			{
				Debug.LogError($"<b>Failed to find Index:</b> No asset found at " +
					$"{c_indexAssetPath}. Check the path/file name.");
				return;
			}

			indexAsset.PopulateUniqueAssets();
		}
	}
#endif
}