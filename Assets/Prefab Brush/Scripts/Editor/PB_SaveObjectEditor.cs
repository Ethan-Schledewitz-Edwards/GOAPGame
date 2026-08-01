using UnityEngine;
using UnityEditor;

namespace ArchieAndrews.PrefabBrush
{
    [CustomEditor(typeof(PB_SaveObject))]
    public class PB_SaveObjectEditor : Editor
    {
        private bool showVariables = false;

        public override void OnInspectorGUI()
        {
            PB_SaveObject t = (PB_SaveObject)target;

            if (GUILayout.Button("Open Brush"))
            {
                PrefabBrush.ShowWindow(t);
            }

            showVariables = EditorGUILayout.Foldout(showVariables,"Show Variables", EditorStyles.foldout);
            if(showVariables)
                base.OnInspectorGUI();
        }
    }
}