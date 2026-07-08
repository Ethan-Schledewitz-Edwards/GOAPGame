using System.Collections.Generic;
using UnityEngine;

public class AIContext
{
	private Dictionary<string, object> m_data = new Dictionary<string, object>();

	public void SetData<T>(string key, T value)
	{
		m_data[key] = value;
	}

	public T GetData<T>(string key, T defaultValue = default)
	{
		if (m_data.TryGetValue(key, out object value))
		{
			return (T)value;
		}
		return defaultValue;
	}

	public void ClearData(string key)
	{
		m_data.Remove(key);
	}

	public void ClearAllData()
	{
		m_data.Clear();
	}
}

public static class AIContextKeys
{
	public const string c_ExecutorTransform = "ExecutorTransform";
	public const string c_InteractionDistance = "InteractionDistance";
	public const string c_InteractionLayer = "InteractionLayer";
	public const string c_TargetTransform = "TargetTransform";
	public const string c_HeldItemID = "HeldItemID";
	public const string c_BlueprintID = "BlueprintID";
}
