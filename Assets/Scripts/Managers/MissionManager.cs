using UnityEngine;

namespace Managers
{
	[RequireComponent(typeof(MissionClock))]
	public class MissionManager : MonoBehaviour
	{
		public static MissionManager Instance;

		private MissionClock m_clock;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticData()
		{
			Instance = null;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;

			m_clock = GetComponent<MissionClock>();
		}

		private void Start()
		{
			StartMission();
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		public void StartMission()
		{
			m_clock.StartClock(1, 6);
		}

		public void EndMission(bool isSuccsess)
		{

		}
	}
}
