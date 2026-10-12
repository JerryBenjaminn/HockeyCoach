namespace HockeyCoach.Harness.Simulation;

/// <summary>
/// Rink dimensions in metres for the Unity replay only (NHL rink, Q-077 default): written to the <c>match --json</c>
/// header so the replay can map the node grid to metres. The simulation never uses these values. If the dimensions
/// move to <c>rink.json</c> (Q-077), the header reads them from there instead.
/// </summary>
public static class ReplayRinkDimensions
{
    /// <summary>Rink length end to end, 200 ft.</summary>
    public const double LengthMetres = 60.96;

    /// <summary>Rink width board to board, 85 ft.</summary>
    public const double WidthMetres = 25.91;

    /// <summary>Distance from the end boards to the goal line, 11 ft.</summary>
    public const double GoalLineFromEndMetres = 3.35;

    /// <summary>Distance from the end boards to the near edge of the blue line, 75 ft.</summary>
    public const double BlueLineFromEndMetres = 22.86;
}
