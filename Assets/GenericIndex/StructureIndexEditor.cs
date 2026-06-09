using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GenericIndex
{
	[CustomEditor(typeof(ScriptableObject), true)]
	public class GenericIndexEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			Type targetType = target.GetType();
			bool isGenericIndex = IsSubclassOfRawGeneric(typeof(GenericIndexBase<>), targetType);

			if (!isGenericIndex)
			{
				DrawDefaultInspector();
				return;
			}

			DrawDefaultInspector();
			GUILayout.Space(15);

			Color originalColor = GUI.backgroundColor;

			GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
			if (GUILayout.Button("Find All Assets & Auto-Assign IDs", GUILayout.Height(40)))
			{
				if (EditorUtility.DisplayDialog("Populate Generic Index?",
					"This will scan your project, append new unique assets, and re-assign all IDs. Proceed?", "Yes", "No"))
				{
					var method = targetType.GetMethod("PopulateUniqueAssets", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
					if (method != null)
					{
						method.Invoke(target, null);
					}
					else
					{
						Debug.LogError("Could not find method 'PopulateUniqueAssets' via reflection.");
					}
				}
			}

			GUILayout.Space(5);

			GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
			if (GUILayout.Button("Auto-Assign IDs (Current Array)", GUILayout.Height(40)))
			{
				if (EditorUtility.DisplayDialog("Auto-Assign IDs?",
					"This will reset all IDs to match the current order of your data array. Proceed?", "Yes", "No"))
				{
					var method = targetType.GetMethod("AssignNewIDs", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
					if (method != null)
					{
						method.Invoke(target, null);
					}
					else
					{
						Debug.LogError("Could not find method 'AssignNewIDs' via reflection. Ensure it is not misspelled.");
					}
				}
			}

			// Restore color state
			GUI.backgroundColor = originalColor;
		}

		// Helper method to check open generic base classes
		private bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
		{
			while (toCheck != null && toCheck != typeof(object))
			{
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (generic == cur)
				{
					return true;
				}
				toCheck = toCheck.BaseType;
			}
			return false;
		}
	}
}