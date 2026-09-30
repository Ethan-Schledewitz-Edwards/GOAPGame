using UnityEditor;
using UnityEngine;

// NOTE: I'm not sure what this is meant to do. If it only makes a field
// readable in the inspector, you can already see private fields by using debug mode.
// I guess there's not really much harm in serializing a field you don't need to,
// but I would caution against it. If this does something else like only make it
// editable in certain situations then I guess that's different.
namespace Utilities.Core
{
#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUI.enabled = false; // Disable editing
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = true;  // Re-enable editing for subsequent fields
		}
	}
#endif
}
