namespace AlIkhsanMedia.Drone.Infrastructure;

public sealed class EngineRestartPolicy
{
    private readonly TimeSpan[] delays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10)];
    public int MaximumAttempts => delays.Length;
    public bool TryGetDelay(int completedAttempts, out TimeSpan delay)
    {
        if ((uint)completedAttempts < delays.Length) { delay = delays[completedAttempts]; return true; }
        delay = default; return false;
    }
}
