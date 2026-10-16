// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run unit a .NET host activated a program in has ENDED: a STOP RUN, or a fatal exception condition that terminates
/// the run unit, reached the outermost activation boundary (owner decision R48; <c>docs/CONFORMANCE.md</c> DOC-A.1-167;
/// DESIGN-external-repository §4.3 "The exception contract" item 3). It is the ONE exception a run-unit ending hands a
/// host. The runtime's own signals for it inside the run unit (<see cref="StopRun"/>,
/// <see cref="Exceptions.CobolFatalException"/>, <see cref="Exceptions.CobolImplementorFatalException"/>) never reach
/// the host.
/// <para>By the time a host sees it, the §14.6.11 actions have run (every open file of the run unit is closed), and the
/// host's ambient run unit is a new one, so the next activation inherits nothing from the run unit that ended. The host
/// process continues: a run-unit ending never exits it (ISO §14.2.3 13): the implementor specifies "the restrictions and
/// mechanisms" for a non-COBOL activator, and this is the mechanism for a run-unit ending).</para>
/// </summary>
/// <param name="exitStatus">The run unit's termination status: the STOP RUN status (ISO §14.9.42.4 5); ERROR ⇒ 1, NORMAL
/// ⇒ 0 with no value, docs/CONFORMANCE.md item 192), or 1 for an abnormal termination.</param>
/// <param name="abnormal">True for an abnormal run-unit termination (ISO §14.6.12), false for a normal one (§14.6.11, a
/// STOP RUN, with an ERROR phrase or not).</param>
/// <param name="message">What ended the run unit; for an abnormal termination, the fatal condition's report.</param>
public sealed class RunUnitEndedException(long exitStatus, bool abnormal, string message) : Exception(message)
{
    /// <summary>The run unit's termination status (see the constructor).</summary>
    public long ExitStatus { get; } = exitStatus;

    /// <summary>True when the run unit terminated abnormally (ISO §14.6.12).</summary>
    public bool Abnormal { get; } = abnormal;
}
