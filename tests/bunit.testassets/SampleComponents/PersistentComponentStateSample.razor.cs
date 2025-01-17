namespace Bunit.TestAssets.SampleComponents;

public partial class PersistentComponentStateSample : ComponentBase
{
	public const string PersistenceKey = "fetchdata";

	public WeatherForecast[] Forecasts { get; private set; }

	[Inject] public PersistentComponentState State { get; set; }

	protected override void OnInitialized()
	{
		State.RegisterOnPersisting(PersistForecasts);
		if (!State.TryTakeFromJson<WeatherForecast[]>(PersistenceKey, out var _))
		{
			Forecasts = CreateForecasts();
		}
		else
		{
			State.PersistAsJson(PersistenceKey, Forecasts);
		}

		WeatherForecast[] CreateForecasts()
		{
			return new WeatherForecast[]
			{
				new WeatherForecast{ Temperature = 42 },
			};
		}
	}

	private Task PersistForecasts()
	{
		State.PersistAsJson(PersistenceKey, Forecasts);
		return Task.CompletedTask;
	}

	private WeatherForecast[] CreateForecasts()
	{
		return new WeatherForecast[]
		{
			new WeatherForecast{ Temperature = 42 },
		};
	}
}

public record class WeatherForecast
{
	public int Temperature { get; set; }
}
