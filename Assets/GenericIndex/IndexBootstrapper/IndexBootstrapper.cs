using System.Collections.Generic;
using UnityEngine;

namespace GenericIndex
{
    public class IndexBootstrapper : MonoBehaviour
    {
		[SerializeField] private List<ScriptableObject> m_indicesToRegister;

		private void Awake()
		{
			foreach (ScriptableObject index in m_indicesToRegister)
			{
				if (index == null) 
					continue;

				if (index is IRegistrableIndex registrableIndex)
				{
					registrableIndex.RegisterSelf();
				}
				else
				{
					Debug.LogError($"IndexBootstrapper attempted to register:{index.name}. This is not a valid index!");
				}
			}
		}
	}
}
