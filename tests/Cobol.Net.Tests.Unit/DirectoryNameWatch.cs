// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
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
    private Exception? _lost;

    /// <summary>Watch the subdirectory names of <paramref name="parent"/> from now on.</summary>
    internal DirectoryNameWatch(string parent)
    {
        _watcher = new FileSystemWatcher(parent) { NotifyFilter = NotifyFilters.DirectoryName, InternalBufferSize = 64 * 1024 };
        _watcher.Renamed += (_, e) => Record($"renamed {e.OldName} -> {e.Name}", deletion: false);
        _watcher.Deleted += (_, e) => Record($"deleted {e.Name}", deletion: true);
        // A watcher whose buffer overflowed has DROPPED reports, so the order it holds is no observation at all: an absent
        // "deleted <name>" would pass vacuously. It ends the wait and is raised as a non-observation below.
        _watcher.Error += (_, e) =>
        {
            lock (_events) _lost ??= e.GetException();
            _deletion.Set();
        };
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
    /// measure of time: it ends the moment the report does, and it is bounded only by the harness's ONE budget
    /// (<see cref="ProcessObserver.TimeoutMs"/>, kb/Work PB3022 — a test sets no literal time limit). A report that never
    /// comes, or a watcher that lost reports, is a NON-OBSERVATION (<see cref="HarnessNonObservationException"/>), never a
    /// verdict on the order.</summary>
    internal void AssertRenamedAwayRatherThanDeletedInPlace(string name)
    {
        if (!HostFile.ShareModesAreMandatory) return;
        if (!_deletion.Wait(ProcessObserver.TimeoutMs))
            throw new HarnessNonObservationException(
                $"NON-OBSERVATION: the host reported no subdirectory deletion within the harness budget ({ProcessObserver.TimeoutMs} ms).");
        string[] seen;
        Exception? lost;
        lock (_events) (seen, lost) = ([.. _events], _lost);
        if (lost is not null)
            throw new HarnessNonObservationException($"NON-OBSERVATION: the directory watcher lost reports ({lost.Message}).");
        Assert.DoesNotContain($"deleted {name}", seen);
        Assert.Contains(seen, e => e.StartsWith($"renamed {name} -> .wolk-gone-", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _deletion.Dispose();
    }
}
