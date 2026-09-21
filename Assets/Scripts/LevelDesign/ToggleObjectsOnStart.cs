using UnityEngine;

public class ToggleObjectsOnStart : MonoBehaviour
{
	[SerializeField] private GameObject[] m_gameObjectsToEnable;
	[SerializeField] private GameObject[] m_gameObjectsToDisable;

	void Start()
    {
		foreach (GameObject i in m_gameObjectsToEnable)
		{
			i.SetActive(true);
		}

		foreach (GameObject i in m_gameObjectsToDisable)
		{
			i.SetActive(false);
		}
	}
}
