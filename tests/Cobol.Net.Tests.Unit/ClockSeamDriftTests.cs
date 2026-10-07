// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Every temporal reading in the greenfield tree goes through ONE seam — <c>RunUnit.Current.Clock</c>
/// (DESIGN-runtime-library §2.7). A direct <c>DateTime[Offset].Now</c> read compiles perfectly, passes every
/// behavioural test (the wall clock and the seam agree on a developer machine running without a pin), and
/// silently splits the run unit onto two clocks: with <c>COBOLNET_CLOCK</c> pinned, the seam-reading
/// functions return the pinned instant while the direct reader returns real now — so SECONDS-PAST-MIDNIGHT
/// and CURRENT-DATE disagree inside one run unit, and no deterministic golden for §15.21.3 r1 / §15.38.4 r1
/// can exist.
/// <para>⛔ THIS IS NOT HYPOTHETICAL — it is R21. <c>CobolDate.CurrentDate</c> and
/// <c>FormattedCurrentDate</c> predated the seam and kept their direct <c>DateTimeOffset.Now</c> reads
/// through every green battery, because no runtime test can distinguish the two clocks when both tick
/// together. A SOURCE-FORM guard is the only shape that can see it.</para>
/// <para>Allowed readers, each the seam's own edge: <c>IO/Clock.cs</c> (<c>SystemClock</c>'s unpinned
/// fallback IS the system-clock read) and <c>IntrinsicBinder</c>'s <c>CompileClock</c> default
/// (WHEN-COMPILED is the COMPILATION timestamp, §15.99.3 r2 — compile-time by definition, not a run-unit
/// reading).</para>
/// </summary>
public sealed class ClockSeamDriftTests
{
    // Any direct system-clock read: DateTime.Now / DateTimeOffset.Now / *.UtcNow / DateTime.Today.
    private static readonly Regex DirectClockRead =
        new(@"\bDateTime(Offset)?\.(Now|UtcNow|Today)\b", RegexOptions.Compiled);

    private static readonly string[] AllowedFiles =
    [
        Path.Combine("Cobol.Net.Runtime", "IO", "Clock.cs"),
        Path.Combine("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "IntrinsicBinder.cs"),
    ];

    [Fact]
    public void NoDirectClockRead_OutsideTheSeam()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            if (rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")) continue;
            if (AllowedFiles.Contains(rel)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (DirectClockRead.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("///", StringComparison.Ordinal))
                    offenders.Add($"{rel}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "A direct system-clock read outside the RunUnit.Clock seam splits the run unit onto two clocks "
            + "(COBOLNET_CLOCK pins one and not the other — R21). Read RunUnit.Current.Clock.Now() instead. "
            + "Offending sites:\n  " + string.Join("\n  ", offenders));
    }
}
