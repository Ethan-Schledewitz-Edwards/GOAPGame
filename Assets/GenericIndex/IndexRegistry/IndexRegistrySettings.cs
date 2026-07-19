using System.Collections.Generic;
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
			Instance = this;
		}
	}
}