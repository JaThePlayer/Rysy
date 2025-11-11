namespace Rysy.Helpers;

/// <summary>
/// Allows multiple sources to create independently managed locks
/// </summary>
public sealed class LockManager {
    private List<ManagedLock> _locks = new(0);

    /// <summary>
    /// Creates a new lock tied to this manager. By default, the lock is *not* active!
    /// </summary>
    public ManagedLock CreateLock() {
        var l = new ManagedLock(this);
        _locks.Add(l);

        return l;
    }

    public void ReleaseLock(ManagedLock l) {
        _locks.Remove(l);
    }

    public bool IsLocked() {
        foreach (var item in _locks) {
            if (item.Active)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Represents a lock created by a <see cref="LockManager"/>
/// </summary>
public sealed class ManagedLock {
    private LockManager? _parent;

    internal ManagedLock(LockManager parent) { _parent = parent; }

    /// <summary>
    /// Toggles whether the lock is active or not. Setting this to false does not remove the lock from the manager.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Toggles whether the lock is active or not. Setting this to false does not remove the lock from the manager.
    /// </summary>
    //This function exists to allow syntax like lock?.SetActive(...)
    public void SetActive(bool locked) {
        Active = locked;
    }

    /// <summary>
    /// Releases the lock from its <see cref="LockManager"/>, so that it is no longer taken into consideration.
    /// </summary>
    public void Release() {
        _parent?.ReleaseLock(this);
        _parent = null;
    }
}