using UnityEngine;

[System.Serializable]
public class EntitySaveData
{
	public string EntityTypeID;
	public Vector3 LocalPosition; // The entities position within their chunk
	public float Health;
}