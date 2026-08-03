using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;using System.IO;

namespace Entities.Savable
{
#if UNITY_EDITOR
	[CustomEditor(typeof(SaveableEntity))]
	public class SaveableEntityEditor : UnityEditor.Editor
	{
		private const string c_IndexAssetPath = "Assets/SaveLoad/SaveData/SavableEntityPrefabDataIndex.asset";
		private const string c_PrefabDataSaveFolderPath = "Assets/SaveLoad/SaveData/SavableEntityPrefabData/";

		private SerializedProperty m_savablePrefabDataProp;
		private SerializedProperty m_guidProp;

		private void OnEnable()
		{
			m_savablePrefabDataProp = serializedObject.FindProperty("m_savablePrefabData");
			m_guidProp = serializedObject.FindProperty("m_guid");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_savablePrefabDataProp);

			if (m_savablePrefabDataProp.objectReferenceValue == null)
			{
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("This entity is missing its Prefab Data. Savable entities must have data assigned to be saved.", MessageType.Warning);

				if (GUILayout.Button("Create & Assign Prefab Data", GUILayout.Height(30)))
				{
					CreateAndAssignPrefabData();
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void CreateAndAssignPrefabData()
		{
			SaveableEntity entity = (SaveableEntity)target;
			GameObject prefabAsset = null;

			PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null && prefabStage.IsPartOfPrefabContents(entity.gameObject))
			{
				// Prefab Edit Mode
				prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
			}
			else
			{
				//Scene View
				prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(entity.gameObject);
			}

			if (prefabAsset == null)
			{
				Debug.LogError("Cannot create Prefab Data: The selected object is neither a Prefab instance in the scene nor being edited in Prefab Mode.");
				return;
			}

			// Ensure the target directory exists
			if (!Directory.Exists(c_PrefabDataSaveFolderPath))
			{
				Directory.CreateDirectory(c_PrefabDataSaveFolderPath);
			}

			string assetName = $"{prefabAsset.name}_SaveableEntityPrefabData.asset";
			string path = AssetDatabase.GenerateUniqueAssetPath(c_PrefabDataSaveFolderPath + assetName);

			SavableEntityPrefabData newData = ScriptableObject.CreateInstance<SavableEntityPrefabData>();

			SerializedObject newDataSO = new SerializedObject(newData);
			SerializedProperty entityPrefabProp = newDataSO.FindProperty("<EntityPrefab>k__BackingField");

			if (entityPrefabProp != null)
			{
				entityPrefabProp.objectReferenceValue = prefabAsset;
			}
			else
			{
				Debug.LogWarning("Could not automatically assign EntityPrefab. You may need to assign it manually.");
			}

			newDataSO.ApplyModifiedProperties();

			AssetDatabase.CreateAsset(newData, path);
			AssetDatabase.SaveAssets();

			m_savablePrefabDataProp.objectReferenceValue = newData;
			serializedObject.ApplyModifiedProperties();

			AddToIndexAndSetID(newData);
			EditorGUIUtility.PingObject(newData); // Highlight the asset in the Project window
		}

		private void AddToIndexAndSetID(SavableEntityPrefabData newData)
		{
			SavableEntityIndex indexAsset = AssetDatabase.LoadAssetAtPath<SavableEntityIndex>(c_IndexAssetPath);

			if (indexAsset == null)
			{
				Debug.LogError($"<b>Failed to find Index:</b> No asset found at {c_IndexAssetPath}. Check the path/file name carefully.");
				return;
			}

			indexAsset.PopulateUniqueAssets();
		}
	}
#endif
}