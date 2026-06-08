using System.Collections.Generic;
using UnityEngine;

namespace UISystems.RecyclingScrollRect
{
	public interface IRecyclableCell<T>
	{
		public RectTransform RectTransform { get; }
		public GameObject GameObject { get; }

		void ConfigureCell(int index, T[] data);
	}
}