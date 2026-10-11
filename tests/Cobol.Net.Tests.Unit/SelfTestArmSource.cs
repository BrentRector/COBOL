// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A SCRIPT SELF-TEST'S ARMS ARE HELD BY NAME FROM ITS SOURCE, AND ITS RUN IS LEFT TO THE RUNNER (kb/Work PB2882):
/// <c>scripts/self_tests.py</c> discovers every script self-test and runs it in the lander's gate, the Linux gate and
/// CI's jobs, where a red arm is a non-zero exit and a hang is bounded by the runner's machine-wide budget place
/// (<see cref="SelfTestDiscoveryDriftTests"/> holds the discovery and that every gate runs the runner). What the runner
/// cannot see is an arm DELETED from a self-test, which still exits 0 — so a drift test names the arms it requires and
/// this reader finds each one among the script's string literals.
/// </summary>
/// <remarks>
/// Until train 1058 four Unit drift tests ran their script's self-test a SECOND time under the test harness's fixed
/// 120 s per-process budget (<c>COBOLNET_RUN_TIMEOUT_MS</c>), and under a lander's whole-population gate beside the WSL
/// Linux gate each went past it: <c>self_tests.py --self-test</c> took 19 s alone and over 120 s twice in train 1058's
/// first gate (a harness non-observation, with every arm passing in the runner). A fixed time limit in a test is a
/// defect; train 1055 fixed the first of them (<see cref="GateSlotDriftTests"/>) and named these as its siblings.
/// <see cref="SelfTestDiscoveryDriftTests.TheArmReader_ReportsADeletedArm_AndFindsASplitLabelWhole"/> sees it fire.
/// </remarks>
internal static class SelfTestArmSource
{
    /// <summary>A closing quote, the line break and indentation of an implicitly concatenated Python literal, and the
    /// next literal's opening quote (an f-string's too).</summary>
    private static readonly Regex AdjacentLiterals = new(@"""\s*\r?\n\s*f?""", RegexOptions.Compiled);

    /// <summary>The script's text with every implicitly concatenated string literal joined into one, so a label written
    /// across two source lines reads as the one line the self-test prints.</summary>
    internal static string Joined(string source) => AdjacentLiterals.Replace(source, "");

    /// <summary>The labels of <paramref name="arms"/> that <paramref name="source"/> no longer declares, in their order.
    /// A label of an f-string arm is written as its source spells it (<c>{_FLEET_GATES} concurrent …</c>).</summary>
    internal static IReadOnlyList<string> Missing(string source, IEnumerable<string> arms)
    {
        string joined = Joined(source);
        return [.. arms.Where(arm => !joined.Contains(arm, StringComparison.Ordinal))];
    }

    /// <summary>Assert that the script at <paramref name="scriptPath"/> still declares every arm of <paramref name="arms"/>.</summary>
    internal static void AssertDeclares(string scriptPath, IEnumerable<string> arms)
    {
        Assert.True(File.Exists(scriptPath), $"the self-test's script is missing: {scriptPath}");
        var missing = Missing(File.ReadAllText(scriptPath), arms);
        Assert.True(missing.Count == 0,
            $"`{Path.GetFileName(scriptPath)} --self-test` no longer declares these arms — a check that has never been seen "
            + $"to fail is not evidence, and a deleted arm still exits 0:\n  {string.Join("\n  ", missing)}");
    }
}
