// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ORDER IN WHICH A DIRECTORY'S SUBDIRECTORIES LOSE THEIR NAMES — the witness seam of kb/Work PB3109. The last
/// holder's CLOSE removes the lock directory, and what a publisher or tester can meet in the middle of it depends on HOW
/// the name goes: a directory deleted in place passes through DELETE-PENDING under its own name (Windows lists it but
/// answers "access denied" to every open inside it and every listing of it), whereas one renamed away first
/// (<c>SidecarLockFiles.RetireDirectory</c>) takes its name out of every prober's reach in one step. That race happens a
/// few times in a thousand and cannot be made to happen on demand, but the operating environment REPORTS the order, so
/// the witness reads the order: a watcher on the parent directory records each subdirectory's rename and deletion, and
/// the test asks whether the lock directory's own name was ever the one deleted.
/// </summary>
internal sealed class DirectoryNameWatch : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly List<string> _events = [];
    private readonly ManualResetEventSlim _deletion = new();

    /// <summary>Watch the subdirectory names of <paramref name="parent"/> from now on.</summary>
    internal DirectoryNameWatch(string parent)
    {
        _watcher = new FileSystemWatcher(parent) { NotifyFilter = NotifyFilters.DirectoryName, InternalBufferSize = 64 * 1024 };
        _watcher.Renamed += (_, e) => Record($"renamed {e.OldName} -> {e.Name}", deletion: false);
        _watcher.Deleted += (_, e) => Record($"deleted {e.Name}", deletion: true);
        _watcher.EnableRaisingEvents = true;
    }

    private void Record(string what, bool deletion)
    {
        lock (_events) _events.Add(what);
        if (deletion) _deletion.Set();
    }

    /// <summary>After the CLOSE under test: on a host whose share modes are mandatory (the only kind with a delete-pending
    /// state, <see cref="HostFile.ShareModesAreMandatory"/>) the lock directory <paramref name="name"/> must have gone by a
    /// RENAME to a retired name and never by a deletion under its own; elsewhere an unlink is atomic and there is nothing
    /// to see. The wait is for the operating environment's report of the retired name's deletion to arrive, never a
    /// measure of time: it ends the moment the report does, and the bound only turns a report that never comes into a
    /// failure.</summary>
    internal void AssertRenamedAwayRatherThanDeletedInPlace(string name)
    {
        if (!HostFile.ShareModesAreMandatory) return;
        Assert.True(_deletion.Wait(TimeSpan.FromMinutes(2)), "The host reported no subdirectory deletion.");
        string[] seen;
        lock (_events) seen = [.. _events];
        Assert.DoesNotContain($"deleted {name}", seen);
        Assert.Contains(seen, e => e.StartsWith($"renamed {name} -> .wolk-gone-", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _deletion.Dispose();
    }
}
