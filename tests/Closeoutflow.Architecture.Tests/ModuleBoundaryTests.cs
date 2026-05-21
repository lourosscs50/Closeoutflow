using System.Reflection;
using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    [Fact]
    public void Shared_Should_Not_Reference_Feature_Modules_Or_Api()
    {
        var references = GetReferencedAssemblyNames(typeof(Result).Assembly);

        Assert.DoesNotContain("Closeoutflow.Api", references);
        Assert.DoesNotContain("Closeoutflow.Modules.Jobs", references);
        Assert.DoesNotContain("Closeoutflow.Modules.Closeouts", references);
    }

    [Fact]
    public void Jobs_Module_Should_Not_Reference_Api_Or_Closeouts_Module()
    {
        var references = GetReferencedAssemblyNames(typeof(Job).Assembly);

        Assert.DoesNotContain("Closeoutflow.Api", references);
        Assert.DoesNotContain("Closeoutflow.Modules.Closeouts", references);
    }

    [Fact]
    public void Closeouts_Module_Should_Not_Reference_Api()
    {
        var references = GetReferencedAssemblyNames(typeof(CloseoutRecord).Assembly);

        Assert.DoesNotContain("Closeoutflow.Api", references);
    }

    [Fact]
    public void Api_Should_Be_The_Composition_Layer_Allowed_To_Reference_Modules()
    {
        var references = GetReferencedAssemblyNames(Assembly.Load("Closeoutflow.Api"));

        Assert.Contains("Closeoutflow.Modules.Jobs", references);
        Assert.Contains("Closeoutflow.Modules.Closeouts", references);
        Assert.Contains("Closeoutflow.Shared", references);
    }

    private static IReadOnlyCollection<string> GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null)
            .Cast<string>()
            .ToArray();
    }
}
