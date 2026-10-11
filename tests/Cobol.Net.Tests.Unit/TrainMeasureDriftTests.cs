// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GATING POLICY IS MEASURED ON NUMBERS THAT ARE RECORDED RIGHT (kb/Work PB2515):
/// <c>scripts/orchestrator/train_measure.py --self-test</c> refuses an inconsistent train record and a train recorded
/// twice, and computes the comparison of the gating modes — changes landed per hour, whole-population runs per train, red trains
/// and their attribution time, finishers created by ejection, CI reds — per gating mode, as every arm it names.
/// </summary>
/// <remarks>
/// The owner made batched gating the rule (2026-10-10, kb/Work PB2515) on what the landers recorded per train against
/// the per-commit whole population (kb/Work PB1708), and every train is still recorded, so the policy stays measured.
/// A summary that miscounted would decide the gate policy on a wrong number, so the self-test runs in every gate and CI job (scripts/self_tests.py) and its arms are
/// held here by name from its source (kb/Work PB2882, PB3022): a self-test reduced to its happy path still exits 0.
/// </remarks>
public sealed class TrainMeasureDriftTests
{
    [Fact]
    public void TheTrainMeasurement_ProvesEveryArm()
    {
        string script = TestRepo.Scripts("orchestrator", "train_measure.py");
        Assert.True(File.Exists(script), $"the train measurement script is missing: {script}");
        SelfTestArmSource.AssertDeclares(script, new[]
                 {
                     "refuses an inconsistent record",
                     "refuses a train recorded twice and rewrites it with --replace",
                     "summarizes changes landed per hour, population runs, red trains, ejections and CI reds per gating mode",
                     "--since narrows the summary",
                 });
    }
}
