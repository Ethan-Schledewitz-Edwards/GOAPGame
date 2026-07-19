using UnityEngine;

namespace ObjectTags
{
	public abstract class ObjectTagBase : ScriptableObject
	{
		[SerializeField, TextArea(10, 5)] private string DeveloperNotes;
	}
}
