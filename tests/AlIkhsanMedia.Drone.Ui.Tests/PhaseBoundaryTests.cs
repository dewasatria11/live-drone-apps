namespace AlIkhsanMedia.Drone.Ui.Tests;
public sealed class PhaseBoundaryTests
{
    [Fact] public void PhaseOneDoesNotIntroduceUiTestDoubles() => Assert.Empty(Directory.GetFiles(AppContext.BaseDirectory, "*Mock*", SearchOption.TopDirectoryOnly));
}
