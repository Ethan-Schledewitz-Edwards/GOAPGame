using UnityEngine;

namespace ObjectTags
{
	[CreateAssetMenu(fileName = "ObjectTag", menuName = "ObjectTag")]
	public class ObjectTag : ScriptableObject
	{
		[TextArea]
		public string DeveloperNotes;
	}
}
