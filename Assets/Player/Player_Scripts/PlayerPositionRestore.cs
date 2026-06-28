using SaveLoad.Data;
using UnityEngine;

public class PlayerPositionRestore : MonoBehaviour
{
	private SaveableEntity m_saveableEntity;
	private PlayerController m_playerController;

	private void Awake()
	{
		m_saveableEntity = GetComponent<SaveableEntity>();
		m_playerController = GetComponent<PlayerController>();

		if (m_saveableEntity != null)
			m_saveableEntity.TransformRestored += OnRestorePosition;
	}

	private void OnDestroy()
	{
		if (m_saveableEntity != null)
			m_saveableEntity.TransformRestored -= OnRestorePosition;
	}

	private void OnRestorePosition(Vector3 position, Quaternion rotation)
	{
		m_playerController.Teleport(position);
		m_playerController.transform.rotation = rotation;
	}
}
