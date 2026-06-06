using System;
using UnityEditor;
using UnityEngine;

namespace GenericIndex
{
	[CustomEditor(typeof(ScriptableObject), true)]
	public class GenericIndexEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			// Draw the fields (the array)
			DrawDefaultInspector();

			// Check via reflection if this object's class derives from GenericIndex<>
			Type targetType = target.GetType();
			bool isGenericIndex = IsSubclassOfRawGeneric(typeof(GenericIndex<>), targetType);

			if (!isGenericIndex) return;

			GUILayout.Space(15);
			GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);

			if (GUILayout.Button("Find All Assets & Auto-Assign IDs", GUILayout.Height(40)))
			{
				if (EditorUtility.DisplayDialog("Populate Generic Index?",
					"This will scan your project, overwrite this array, and reset all IDs. Proceed?", "Yes", "No"))
				{
					// Find and invoke the PopulateAndAssignIDs method dynamically
					var method = targetType.GetMethod("PopulateAndAssignIDs");
					if (method != null)
					{
						method.Invoke(target, null);
					}
				}
			}
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