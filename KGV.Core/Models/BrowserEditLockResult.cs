namespace KGV.Core.Models;

public sealed record BrowserEditLockResult(bool Acquired, string LockedByDisplayName);
