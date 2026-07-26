using Managers;
using System.Linq;
using TMPro;
using UnityEngine;
using WorldLighting;

public class ClockElement : UIElement
{
	private IGameClock m_gameClock;

	[SerializeField] private TextMeshProUGUI m_clockText;


	private void Start()
	{
		m_gameClock = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
				.OfType<IGameClock>()
				.FirstOrDefault();

		if (m_gameClock == null)
		{
			Debug.LogError($"{gameObject.name}: Missing an {nameof(IGameClock)} reference in scene.", this);
			return;
		}

		m_gameClock.ClockUpdated += OnClockUpdated;

		// Initial display update
		(int currentHour, int currentMinute) = m_gameClock.GetTimeOfDay();
		OnClockUpdated(currentHour, currentMinute);
	}

	private void OnEnable()
	{
		if (m_gameClock != null)
		{
			m_gameClock.ClockUpdated -= OnClockUpdated;
			m_gameClock.ClockUpdated += OnClockUpdated;

			(int currentHour, int currentMinute) = m_gameClock.GetTimeOfDay();
			OnClockUpdated(currentHour, currentMinute);
		}
	}

	private void OnDisable()
	{
		if (m_gameClock != null)
		{
			m_gameClock.ClockUpdated -= OnClockUpdated;
		}
	}

	private void OnClockUpdated(int hour, int minute)
	{
		string amPm = hour >= 12 ? "PM" : "AM";
		int displayHour = hour % 12;
		if (displayHour == 0)
			displayHour = 12;

		m_clockText.text = $"{displayHour}:{minute:D2} {amPm}";
	}
}
