namespace HockeyCoach.Sim.Tactics
{
    /// <summary>Data names of <see cref="SystemRole"/> and <see cref="PuckState"/> (data-schema.md, Puolustusjärjestelmät).</summary>
    public static class SystemNames
    {
        /// <summary>All roles in order F1, F2, F3, D1, D2.</summary>
        public static readonly SystemRole[] AllRoles = { SystemRole.F1, SystemRole.F2, SystemRole.F3, SystemRole.D1, SystemRole.D2 };

        /// <summary>Parses a role name (ordinal).</summary>
        public static bool TryParseRole(string name, out SystemRole role)
        {
            switch (name)
            {
                case "F1": role = SystemRole.F1; return true;
                case "F2": role = SystemRole.F2; return true;
                case "F3": role = SystemRole.F3; return true;
                case "D1": role = SystemRole.D1; return true;
                case "D2": role = SystemRole.D2; return true;
                default: role = default; return false;
            }
        }

        /// <summary>The data name of a role.</summary>
        public static string ToName(SystemRole role)
        {
            return role.ToString();
        }

        /// <summary>Parses a puck state name: <c>controlled</c> or <c>loose</c> (ordinal).</summary>
        public static bool TryParsePuckState(string name, out PuckState state)
        {
            switch (name)
            {
                case "controlled": state = PuckState.Controlled; return true;
                case "loose": state = PuckState.Loose; return true;
                default: state = default; return false;
            }
        }
    }
}
