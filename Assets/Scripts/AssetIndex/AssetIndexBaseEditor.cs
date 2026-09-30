using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetIndex.Core
{
	/// <summary>
	/// AI generated editor script.
	/// </summary>
#if UNITY_EDITOR
	[CustomEditor(typeof(ScriptableObject), true)]
	public class AssetIndexBaseEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			Type targetType = target.GetType();
			bool isGenericIndex = IsSubclassOfGenericIndex(typeof(AssetIndexBase<>), targetType);

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
					var method = targetType.GetMethod("PopulateUniqueAssets", BindingFlags.Public |
						BindingFlags.Instance |
						BindingFlags.FlattenHierarchy);
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

			// Restore color state
			GUI.backgroundColor = originalColor;
		}

		private bool IsSubclassOfGenericIndex(Type generic, Type toCheck)
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
#endif
}