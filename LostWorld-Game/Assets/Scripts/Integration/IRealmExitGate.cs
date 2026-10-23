namespace LostWorlds.Person2.Integration
{
    /// <summary>Optional world-owned condition. P11 still owns all transitions and duplicate guards.</summary>
    public interface IRealmExitGate { bool CanExit { get; } }
}
