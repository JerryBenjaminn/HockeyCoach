namespace HockeyCoach.Harness.Batch;

/// <summary>
/// Skill-vs-luck figures of one matchup from team A's point of view (D-073, D-077 item 8): record, points share with a
/// pair-level 95 % confidence interval, upsets, pair consistency, venue split, the goal-difference variance split into
/// chance creation (xG difference) and finishing luck, and how often the xG winner wins.
/// </summary>
public sealed class SkillLuck
{
    /// <summary>Two-sided 95 % normal quantile (statistics constant, not a balance value).</summary>
    public const double Z95 = 1.959963984540054;

    private SkillLuck()
    {
    }

    /// <summary>Matches.</summary>
    public int Matches { get; private init; }

    /// <summary>Pairs.</summary>
    public int Pairs { get; private init; }

    /// <summary>A's wins.</summary>
    public int Wins { get; private init; }

    /// <summary>Ties.</summary>
    public int Ties { get; private init; }

    /// <summary>A's losses.</summary>
    public int Losses { get; private init; }

    /// <summary>A's points share, tie = half (D-073).</summary>
    public double PointsShare { get; private init; }

    /// <summary>Lower end of the pair-level 95 % CI of <see cref="PointsShare"/>.</summary>
    public double CiLow { get; private init; }

    /// <summary>Upper end of the pair-level 95 % CI.</summary>
    public double CiHigh { get; private init; }

    /// <summary>The favourite: "A" when <see cref="PointsShare"/> ≥ 0.5, else "B".</summary>
    public string Favourite { get; private init; } = "A";

    /// <summary>Matches the favourite lost, per match.</summary>
    public double UpsetRate { get; private init; }

    /// <summary>Pairs where one team won both matches.</summary>
    public double SweepShare { get; private init; }

    /// <summary>Pairs where each team won one match.</summary>
    public double SplitShare { get; private init; }

    /// <summary>Pairs where the favourite got more points than the underdog.</summary>
    public double FavouritePairShare { get; private init; }

    /// <summary>Home wins per match.</summary>
    public double HomeWinShare { get; private init; }

    /// <summary>Away wins per match.</summary>
    public double AwayWinShare { get; private init; }

    /// <summary>Home points share (tie = half).</summary>
    public double HomePointsShare { get; private init; }

    /// <summary>Mean goal difference A − B.</summary>
    public double MeanGoalDiff { get; private init; }

    /// <summary>Mean xG difference A − B.</summary>
    public double MeanXgDiff { get; private init; }

    /// <summary>Variance of the goal difference.</summary>
    public double VarGoalDiff { get; private init; }

    /// <summary>Var(xG difference) / Var(goal difference).</summary>
    public double ChanceShare { get; private init; }

    /// <summary>Var(finishing luck = goal diff − xG diff) / Var(goal difference).</summary>
    public double FinishingShare { get; private init; }

    /// <summary>2 Cov(xG diff, finishing luck) / Var(goal difference).</summary>
    public double CovarianceShare { get; private init; }

    /// <summary>Matches with a different xG for the two teams.</summary>
    public int XgDecided { get; private init; }

    /// <summary>Of <see cref="XgDecided"/>: share won by the team with more xG.</summary>
    public double XgWinnerWins { get; private init; }

    /// <summary>Of <see cref="XgDecided"/>: share tied.</summary>
    public double XgWinnerTies { get; private init; }

    /// <summary>Goals per match, both teams.</summary>
    public double GoalsPerMatch { get; private init; }

    /// <summary>Variance of goals per match, both teams.</summary>
    public double GoalsVariance { get; private init; }

    /// <summary>Variance / mean of team goals per match (1 = Poisson).</summary>
    public double TeamGoalsDispersion { get; private init; }

    /// <summary>Stalled matches.</summary>
    public int Stalled { get; private init; }

    /// <summary>Computes the figures; <paramref name="lines"/> are in play order, two per pair.</summary>
    public static SkillLuck From(IReadOnlyList<MatchLine> lines)
    {
        int n = lines.Count;
        if (n == 0)
        {
            return new SkillLuck();
        }

        int wins = lines.Count(l => l.GoalsA > l.GoalsB);
        int ties = lines.Count(l => l.GoalsA == l.GoalsB);
        double share = lines.Average(l => l.PointsA);
        double[] pairPoints = lines.GroupBy(l => l.Pair).OrderBy(g => g.Key).Select(g => g.Average(l => l.PointsA)).ToArray();
        double se = pairPoints.Length > 1 ? Math.Sqrt(Variance(pairPoints) / pairPoints.Length) : 0.0;
        bool aFavourite = share >= 0.5;
        int favouriteLosses = aFavourite ? lines.Count(l => l.GoalsA < l.GoalsB) : wins;
        int pairs = pairPoints.Length;
        var pairGames = lines.GroupBy(l => l.Pair).OrderBy(g => g.Key).Select(g => g.ToArray()).ToArray();
        int sweeps = pairGames.Count(g => g.Length == 2 && ((g[0].GoalsA > g[0].GoalsB && g[1].GoalsA > g[1].GoalsB) || (g[0].GoalsA < g[0].GoalsB && g[1].GoalsA < g[1].GoalsB)));
        int splits = pairGames.Count(g => g.Length == 2 && ((g[0].GoalsA > g[0].GoalsB && g[1].GoalsA < g[1].GoalsB) || (g[0].GoalsA < g[0].GoalsB && g[1].GoalsA > g[1].GoalsB)));
        int favouritePairs = pairPoints.Count(p => aFavourite ? p > 0.5 : p < 0.5);
        int homeWins = lines.Count(l => l.TeamAIsHome ? l.GoalsA > l.GoalsB : l.GoalsB > l.GoalsA);
        int awayWins = lines.Count(l => l.TeamAIsHome ? l.GoalsA < l.GoalsB : l.GoalsB < l.GoalsA);

        double[] gd = lines.Select(l => (double)(l.GoalsA - l.GoalsB)).ToArray();
        double[] xgd = lines.Select(l => l.XgA - l.XgB).ToArray();
        double[] luck = gd.Zip(xgd, (g, x) => g - x).ToArray();
        double varGd = Variance(gd);
        double varXgd = Variance(xgd);
        double varLuck = Variance(luck);
        double cov = Covariance(xgd, luck);
        MatchLine[] decided = lines.Where(l => l.XgA != l.XgB).ToArray();
        int xgWinnerWins = decided.Count(l => (l.XgA > l.XgB && l.GoalsA > l.GoalsB) || (l.XgB > l.XgA && l.GoalsB > l.GoalsA));
        int xgTies = decided.Count(l => l.GoalsA == l.GoalsB);
        double[] totals = lines.Select(l => (double)(l.GoalsA + l.GoalsB)).ToArray();
        double[] teamGoals = lines.SelectMany(l => new[] { (double)l.GoalsA, l.GoalsB }).ToArray();
        double teamMean = teamGoals.Average();

        return new SkillLuck
        {
            Matches = n,
            Pairs = pairs,
            Wins = wins,
            Ties = ties,
            Losses = n - wins - ties,
            PointsShare = share,
            CiLow = Math.Max(0.0, share - (Z95 * se)),
            CiHigh = Math.Min(1.0, share + (Z95 * se)),
            Favourite = aFavourite ? "A" : "B",
            UpsetRate = favouriteLosses / (double)n,
            SweepShare = pairs == 0 ? 0.0 : sweeps / (double)pairs,
            SplitShare = pairs == 0 ? 0.0 : splits / (double)pairs,
            FavouritePairShare = pairs == 0 ? 0.0 : favouritePairs / (double)pairs,
            HomeWinShare = homeWins / (double)n,
            AwayWinShare = awayWins / (double)n,
            HomePointsShare = (homeWins + (0.5 * ties)) / n,
            MeanGoalDiff = gd.Average(),
            MeanXgDiff = xgd.Average(),
            VarGoalDiff = varGd,
            ChanceShare = varGd > 0 ? varXgd / varGd : 0.0,
            FinishingShare = varGd > 0 ? varLuck / varGd : 0.0,
            CovarianceShare = varGd > 0 ? 2.0 * cov / varGd : 0.0,
            XgDecided = decided.Length,
            XgWinnerWins = decided.Length == 0 ? 0.0 : xgWinnerWins / (double)decided.Length,
            XgWinnerTies = decided.Length == 0 ? 0.0 : xgTies / (double)decided.Length,
            GoalsPerMatch = totals.Average(),
            GoalsVariance = Variance(totals),
            TeamGoalsDispersion = teamMean > 0 ? Variance(teamGoals) / teamMean : 0.0,
            Stalled = lines.Count(l => l.Stalled),
        };
    }

    /// <summary>Sample variance (n − 1); 0 for fewer than two values.</summary>
    public static double Variance(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0.0;
        }

        double mean = values.Average();
        return values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
    }

    /// <summary>Sample covariance (n − 1); 0 for fewer than two values.</summary>
    public static double Covariance(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count < 2)
        {
            return 0.0;
        }

        double mx = x.Average();
        double my = y.Average();
        double sum = 0.0;
        for (int i = 0; i < x.Count; i++)
        {
            sum += (x[i] - mx) * (y[i] - my);
        }

        return sum / (x.Count - 1);
    }
}
