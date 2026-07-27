using UnityEngine;

namespace Managers
{
	[RequireComponent(typeof(MissionClock))]
	public class MissionManager : MonoBehaviour
	{
		public static MissionManager Instance;

		private MissionClock m_clock;

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else Destroy(Instance);

			m_clock = GetComponent<MissionClock>();
		}

		private void Start()
		{
			StartMission();
		}

		public void StartMission()
		{
			m_clock.StartClock(1);
		}

		public void EndMission(bool isSucsess)
		{

		}
	}
}
