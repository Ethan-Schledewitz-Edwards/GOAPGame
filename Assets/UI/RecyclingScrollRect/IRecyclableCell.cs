using System.Collections.Generic;
using UnityEngine;

namespace UISystems.RecyclingScrollRect
{
	public interface IRecyclableCell<T>
	{
		public GameObject Prefab { get; }
		public GameObject GameObject { get; }
		public RectTransform RectTransform { get; }

		void ConfigureCell(int index, T[] data);
	}
}