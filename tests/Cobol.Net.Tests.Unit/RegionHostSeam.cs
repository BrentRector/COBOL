// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;

namespace CobolNet.Tests.Unit;

/// <summary>Which host of the region locks (<see cref="RegionLocks"/>, kb/Work PB2936) a cross-run-unit scenario runs on.</summary>
public enum RegionHostKind
{
    /// <summary>The host of the machine the test runs on: <c>LockFileEx</c> on Windows, open-file-description locks on
    /// Linux, the lock files on macOS.</summary>
    OfThisProcess,
    /// <summary>The lock-file host (<see cref="SidecarRegionLocks"/>), whichever machine this is. It is plain managed
    /// code over <see cref="FileShare"/>, so the macOS expression is held to the very scenarios Linux and Windows are
    /// without a Mac.</summary>
    LockFiles,
}

/// <summary>Scopes a test's connectors to one <see cref="RegionHostKind"/> (<see cref="HostRegionLocks.Forcing"/>).</summary>
internal static class RegionHostSeam
{
    /// <summary>Dispose the result when the scenario ends; null when the machine's own host is the one under test.</summary>
    internal static IDisposable? Use(RegionHostKind kind) =>
        kind == RegionHostKind.LockFiles ? HostRegionLocks.Forcing(SidecarRegionLocks.Host) : null;
}
