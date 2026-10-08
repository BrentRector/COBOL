// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BATCHED-GATING TRIAL IS DECIDED ON NUMBERS THAT ARE RECORDED RIGHT (kb/Work PB2515):
/// <c>scripts/orchestrator/train_measure.py --self-test</c> refuses an inconsistent train record and a train recorded
/// twice, and computes the trial's comparison — changes landed per hour, whole-population runs per train, red trains
/// and their attribution time, finishers created by ejection, CI reds — per gating mode, as every arm it names.
/// </summary>
/// <remarks>
/// The owner adopted batched gating as a trial (2026-10-07, until Sat 2026-10-10 10:00 PDT) and decides whether it stays
/// on what the landers recorded per train against the per-commit whole population (kb/Work PB1708). A summary that
/// miscounted would decide the gate policy on a wrong number, so the self-test runs in every Unit run and its arms are
/// asserted by name: a self-test reduced to its happy path still exits 0. This test measures nothing and times nothing.
/// </remarks>
public sealed class TrainMeasureDriftTests
{
    [Fact]
    public void TheTrainMeasurement_ProvesEveryArm()
    {
        string script = TestRepo.Scripts("orchestrator", "train_measure.py");
        Assert.True(File.Exists(script), $"the train measurement script is missing: {script}");
        var r = PythonInstrument.Run(script, "--self-test");

        Assert.True(r.ExitCode == 0 && r.Stdout.Contains("train_measure SELF-TEST: PASS", StringComparison.Ordinal),
            $"train_measure.py --self-test failed (exit {r.ExitCode}).\n{r.Stdout}\n{r.Stderr}");
        foreach (string arm in new[]
                 {
                     "refuses an inconsistent record",
                     "refuses a train recorded twice and rewrites it with --replace",
                     "summarizes changes landed per hour, population runs, red trains, ejections and CI reds per gating mode",
                     "--since narrows the summary",
                 })
        {
            Assert.True(r.Stdout.Contains("PASS  " + arm, StringComparison.Ordinal),
                $"`train_measure.py --self-test` no longer passes '{arm}'.\n{r.Stdout}{r.Stderr}");
        }
    }
}
