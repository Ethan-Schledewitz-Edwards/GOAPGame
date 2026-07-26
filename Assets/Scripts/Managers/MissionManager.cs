using UnityEngine;

namespace Managers
{
	[RequireComponent(typeof(MissionClock))]
	public class MissionManager : MonoBehaviour
	{
		public static MissionManager Instance;

		private MissionClock m_Clock;

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else Destroy(Instance);

			m_Clock = GetComponent<MissionClock>();
		}

		public void StartMission()
		{

		}

		public void EndMission(bool isSucsess)
		{

		}
	}
}
