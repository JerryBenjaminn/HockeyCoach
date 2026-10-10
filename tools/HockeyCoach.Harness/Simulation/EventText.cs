using System.Globalization;
using HockeyCoach.Sim.Events;

namespace HockeyCoach.Harness.Simulation;

/// <summary>Readable one-line text for each event.</summary>
public sealed class EventText : ISimEventVisitor<string>
{
    private readonly Func<int, string> _name;

    /// <summary>Creates the formatter.</summary>
    /// <param name="name">Player id → display name.</param>
    public EventText(Func<int, string> name)
    {
        _name = name;
    }

    /// <summary>The full line: time, period, strength, play/system and the event's own fields.</summary>
    public string Line(SimEvent e)
    {
        EventContext c = e.Context;
        string tactics = (c.PlayId ?? "-") + " vs " + (c.SystemId ?? "-");
        return "P" + c.Period + " " + Clock(c.Time) + " " + c.Strength + " [" + tactics + "] " + e.Accept(this);
    }

    /// <inheritdoc />
    public string Visit(FaceoffEvent e) => "Faceoff: " + _name(e.HomeCentreId) + " vs " + _name(e.AwayCentreId) + ", won by " + e.Winner + " (" + e.Location + ")";

    /// <inheritdoc />
    public string Visit(ControlledZoneEntryEvent e) => "Zone entry: " + _name(e.CarrierId) + " " + e.Method + " " + e.Numbers + ", " + e.Outcome;

    /// <inheritdoc />
    public string Visit(DumpInEvent e) => "Dump-in: " + _name(e.ShooterId) + ", battle " + e.BattleOutcome;

    /// <inheritdoc />
    public string Visit(PassEvent e) => "Pass: " + _name(e.PasserId) + " → " + _name(e.ReceiverId) + (e.UnderPressure ? " under pressure" : string.Empty) + (e.Succeeded ? ", complete" : ", failed");

    /// <inheritdoc />
    public string Visit(ShotEvent e) => "Shot: " + _name(e.ShooterId) + " xG " + e.Xg.ToString("0.000", CultureInfo.InvariantCulture)
        + " " + (e.ChanceClass?.ToString() ?? "unclassed") + " " + e.ChanceType + (e.UnderPressure ? " under pressure" : string.Empty)
        + " → " + e.Outcome.ToString().ToUpperInvariant() + (e.StoppedById.HasValue ? " by " + _name(e.StoppedById.Value) : string.Empty);

    /// <inheritdoc />
    public string Visit(TurnoverEvent e) => "Turnover: " + _name(e.LoserId) + " lost to " + _name(e.TakerId) + " at node " + e.NodeId;

    /// <inheritdoc />
    public string Visit(PuckBattleEvent e) => "Puck battle: " + string.Join("+", e.AttackerIds.Select(_name)) + " vs " + string.Join("+", e.DefenderIds.Select(_name)) + ", " + e.Outcome;

    /// <inheritdoc />
    public string Visit(StoppageEvent e) => "STOPPAGE: " + e.Reason;

    /// <inheritdoc />
    public string Visit(LineChangeEvent e) => "Line change: " + string.Join(",", e.OutgoingIds.Select(_name)) + " → " + string.Join(",", e.IncomingIds.Select(_name));

    private static string Clock(double seconds)
    {
        int whole = (int)Math.Floor(seconds);
        return (whole / 60).ToString("00", CultureInfo.InvariantCulture) + ":" + (whole % 60).ToString("00", CultureInfo.InvariantCulture);
    }
}
