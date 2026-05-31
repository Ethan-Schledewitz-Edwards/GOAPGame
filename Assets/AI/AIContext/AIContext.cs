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
}
