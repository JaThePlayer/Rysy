namespace Rysy;

public static class Time {
    public static float Delta { get; private set; }

    /// <summary>
    /// Time delta, unscaled by TimeScale
    /// </summary>
    public static float RawDelta { get; private set; }
    public static float TimeScale {
        get => _timeScale;
        set {
            Delta = RawDelta * value;
            _timeScale = value;
        }
    }

    private static float _timeScale = 1f;

    /// <summary>
    /// How much time has elapsed since the start of the game
    /// </summary>
    public static float Elapsed { get; private set; } = 0f;

    public static float RawElapsed { get; private set; } = 0f;

    internal static void Update(float deltaSeconds) {
        RawDelta = deltaSeconds;
        Delta = RawDelta * TimeScale;
        Elapsed += Delta;
        RawElapsed += RawDelta;
    }
}

