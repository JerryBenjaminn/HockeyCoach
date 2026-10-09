using System.Reflection;
using System.Xml.Linq;
using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Tests;

/// <summary>
/// Guards the structure rules of docs/tech-spec.md and CLAUDE.md: no runtime dependencies in Sim/AI,
/// Unity-compatible project settings, and module dependencies flowing top-down.
/// </summary>
public class ArchitectureTests
{
    private const string SimRoot = "HockeyCoach.Sim";

    /// <summary>
    /// Module order from docs/tech-spec.md, top to bottom: a module may use modules below it, never above.
    /// Config is placed directly above Model because check weights reference Model's stat types;
    /// its exact place in the order is an open question.
    /// </summary>
    private static readonly string[] ModuleOrder =
    {
        "Match", "Shift", "Checks", "State", "Tactics", "Config", "Model", "Events", "Random",
    };

    private static readonly Assembly SimAssembly = typeof(Pcg32).Assembly;
    private static readonly Assembly AiAssembly = Assembly.Load("HockeyCoach.AI");

    [Fact]
    public void Sim_ReferencesOnlyNetStandard()
    {
        string[] references = SimAssembly.GetReferencedAssemblies().Select(a => a.Name!).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "netstandard" }, references);
    }

    [Fact]
    public void AI_ReferencesOnlyNetStandardAndSim()
    {
        string[] references = AiAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();

        Assert.All(references, name => Assert.Contains(name, new[] { "netstandard", SimRoot }));
    }

    [Theory]
    [InlineData("src/HockeyCoach.Sim/HockeyCoach.Sim.csproj")]
    [InlineData("src/HockeyCoach.AI/HockeyCoach.AI.csproj")]
    public void UnityCompatibleProjects_TargetNetStandard21WithCSharp9(string relativePath)
    {
        XDocument project = XDocument.Load(Path.Combine(FindRepoRoot(), relativePath));
        string Property(string name) => project.Descendants(name).Select(e => e.Value).SingleOrDefault() ?? string.Empty;

        Assert.Equal("netstandard2.1", Property("TargetFramework"));
        Assert.Equal("9.0", Property("LangVersion"));
        Assert.Equal("disable", Property("ImplicitUsings"));
        Assert.DoesNotContain(project.Descendants("PackageReference"), p => p.Attribute("PrivateAssets")?.Value != "all");
    }

    [Fact]
    public void SimTypes_LiveInKnownModules()
    {
        foreach (Type type in SimTypes())
        {
            Assert.True(ModuleOf(type) != null, type.FullName + " is not in a module listed in docs/tech-spec.md");
        }
    }

    [Fact]
    public void SimModules_DependOnlyOnModulesBelowThem()
    {
        var violations = new List<string>();
        foreach (Type type in SimTypes())
        {
            int from = Array.IndexOf(ModuleOrder, ModuleOf(type));
            foreach (Type used in SignatureDependencies(type))
            {
                string? usedModule = ModuleOf(used);
                if (usedModule == null)
                {
                    continue;
                }

                int to = Array.IndexOf(ModuleOrder, usedModule);
                if (to < from)
                {
                    violations.Add(type.FullName + " (" + ModuleOf(type) + ") uses " + used.FullName + " (" + usedModule + ")");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations.Distinct().OrderBy(v => v, StringComparer.Ordinal)));
    }

    private static IEnumerable<Type> SimTypes()
    {
        return SimAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith(SimRoot, StringComparison.Ordinal))
            .Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false));
    }

    private static string? ModuleOf(Type type)
    {
        if (type.Assembly != SimAssembly || type.Namespace == null || !type.Namespace.StartsWith(SimRoot + ".", StringComparison.Ordinal))
        {
            return null;
        }

        string module = type.Namespace.Substring(SimRoot.Length + 1).Split('.')[0];
        return Array.IndexOf(ModuleOrder, module) >= 0 ? module : null;
    }

    /// <summary>Types used in the public and private signatures of <paramref name="type"/> (not method bodies).</summary>
    private static IEnumerable<Type> SignatureDependencies(Type type)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var found = new List<Type>();
        if (type.BaseType != null)
        {
            found.Add(type.BaseType);
        }

        found.AddRange(type.GetInterfaces());
        found.AddRange(type.GetFields(all).Select(f => f.FieldType));
        found.AddRange(type.GetProperties(all).Select(p => p.PropertyType));
        foreach (MethodBase method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
        {
            found.AddRange(method.GetParameters().Select(p => p.ParameterType));
            if (method is MethodInfo info)
            {
                found.Add(info.ReturnType);
            }
        }

        return found.SelectMany(Unwrap);
    }

    private static IEnumerable<Type> Unwrap(Type type)
    {
        if (type.HasElementType)
        {
            return Unwrap(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            return new[] { type.GetGenericTypeDefinition() }.Concat(type.GetGenericArguments().SelectMany(Unwrap));
        }

        return new[] { type };
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HockeyCoach.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("HockeyCoach.sln not found above " + AppContext.BaseDirectory);
    }
}
