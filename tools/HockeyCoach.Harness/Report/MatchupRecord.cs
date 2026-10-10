namespace HockeyCoach.Harness.Report;

/// <summary>
/// Wins, ties and losses of one side of a matchup, e.g. the smart AI against the random AI
/// (<c>smartVsRandomWinRate</c>, D-073).
/// </summary>
public sealed class MatchupRecord
{
    /// <summary>Matches the side won.</summary>
    public int Wins { get; private set; }

    /// <summary>Tied matches.</summary>
    public int Ties { get; private set; }

    /// <summary>Matches the side lost.</summary>
    public int Losses { get; private set; }

    /// <summary>All matches added.</summary>
    public int Matches => Wins + Ties + Losses;

    /// <summary>D-073: a tie counts as half a win, (wins + ties / 2) / matches; null without matches.</summary>
    public double? WinRate => Matches > 0 ? (Wins + (0.5 * Ties)) / Matches : null;

    /// <summary>Adds one match from the side's point of view.</summary>
    /// <param name="goalsFor">The side's goals.</param>
    /// <param name="goalsAgainst">The opponent's goals.</param>
    public void Add(int goalsFor, int goalsAgainst)
    {
        if (goalsFor > goalsAgainst)
        {
            Wins++;
        }
        else if (goalsFor < goalsAgainst)
        {
            Losses++;
        }
        else
        {
            Ties++;
        }
    }
}
