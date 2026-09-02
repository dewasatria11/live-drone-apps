namespace AlIkhsanMedia.Drone.Core.Tests;
public sealed class DependencyDirectionTests
{
    [Fact]
    public void CoreAssemblyHasNoForbiddenProductReferences()
    {
        var references = typeof(StreamSlotId).Assembly.GetReferencedAssemblies().Select(static x => x.Name).ToArray();
        Assert.DoesNotContain("AlIkhsanMedia.Drone.App", references);
        Assert.DoesNotContain("AlIkhsanMedia.Drone.Infrastructure", references);
        Assert.DoesNotContain("AlIkhsanMedia.Drone.SetupPortal", references);
        Assert.DoesNotContain("PresentationFramework", references);
    }
}
