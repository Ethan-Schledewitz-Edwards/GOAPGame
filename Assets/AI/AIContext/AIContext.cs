using System.Collections.Generic;
using UnityEngine;

public class AIContext
{
	// NOTE: If we use fields, then this is not needed. Just reset by newing the class.
	// Using a generic object dictionary may feel extensible but it is very fuzzy and not at all type safe.
	// This is essentially javascript, and if you've every used javascript, you'd know it's a
	// bad idea for games. It makes it easy to misuse and hard to track down mistakes.
	// This will cause bugs later, mark my words. I recommend switching this out for specialized
	// arrays, dictionaries, bitfields, or whatever you like.
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

	public Dictionary<string, object> GetDataSet()
	{
		return m_data;
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

// NOTE: This should either be an enum or just fields in AIContext.
// Fields is likely the better option, but an enum is just a more
// efficient way of doing what it's doing currently.
// NOTE 2: I came back to this after reading the item tag filter code.
// It makes more sense why it was done this way now, since an enum wouldn't work.
// but it should still be done with separate fields instead. The item tag filters
// should probably be an array or dictionary (fun fact, in this situation a dictionary
// would probably perform worse).
public static class AIContextKeys
{
	public const string c_CurrentBTNode = "CurrentBTNode";
	public const string c_ExecutorTransform = "ExecutorTransform";
	public const string c_InteractionDistanceSqrt = "InteractionDistance";
	public const string c_JobSearchRange = "JobSearchRange";
	public const string c_InteractionLayer = "InteractionLayer";
	public const string c_TargetTransform = "TargetTransform";
	public const string c_TargetDestination = "TargetDestination";
	public const string c_HeldItemID = "HeldItemID";
	public const string c_StructureSettlementID = "StructureSettlementID";
	public const string c_StructureID = "StructureID";
	public const string c_ItemToFindID = "ItemToFind";
	public const string c_ItemTagFilterPrefix = "itemTagFilter_";
	public const string c_ExecutorFaction = "ExecutorFaction";
	public const string c_AssignedInteractionPosition = "AssignedInteractionPosition";
	public const string c_ReservationCleanup = "ReservationCleanup";
}
