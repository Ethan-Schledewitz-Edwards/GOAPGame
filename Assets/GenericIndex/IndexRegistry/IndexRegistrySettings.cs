using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GenericIndex
{
	[CreateAssetMenu(fileName = "IndexRegistrySettings", menuName = "Settings/Index Registry Settings")]
	public class IndexRegistrySettings : ScriptableObject
	{
		public static IndexRegistrySettings Instance { get; private set; }

		[SerializeField] private List<ScriptableObject> m_indicesToRegister;
		public IReadOnlyList<ScriptableObject> IndicesToRegister => m_indicesToRegister;

		private void OnEnable()
		{
			if (Instance == null)
				Instance = this;
		}

#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void EditorPlayModeRebind()
		{
			if (Instance == null)
			{
				string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(IndexRegistrySettings)}");
				if (guids.Length > 0)
				{
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
					Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<IndexRegistrySettings>(path);
				}
			}
		}
#endif
	}
}