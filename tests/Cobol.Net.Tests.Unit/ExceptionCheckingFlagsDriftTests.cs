// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The invariant that keeps §14.9.28.4 GR14's "implicit PUSH ALL followed by TURN OFF ALL" COMPLETE as
/// exception conditions are added.
///
/// <para><b>Why this test exists.</b> GR14 disables EVERY ambient checking flag for the duration of an
/// exception-checking PERFORM's handler bodies. Expressed as a hand-maintained save/restore list, the failure
/// mode is silent: a condition added later that forgets to join the list simply stops obeying GR14 — no test
/// fails, no diagnostic fires, and the defect surfaces as a program that terminates abnormally inside a handler
/// that the standard says cannot raise. So the flags live in ONE <see cref="CheckingFlags"/> struct and PUSH ALL
/// is a struct copy. These tests fail the moment that stops being true — which is the only thing that makes
/// "covered by construction" a fact rather than an intention.</para>
/// </summary>
public sealed class ExceptionCheckingFlagsDriftTests
{
    private static PropertyInfo[] EngineCheckingFlags() =>
        [.. typeof(ExceptionEngine)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(bool) && p.Name.EndsWith("Checking", StringComparison.Ordinal))];

    [Fact]   // Every engine flag is BACKED by the struct — none was declared as loose auto-property storage.
    public void EveryCheckingFlag_IsBackedByTheStruct()
    {
        var fields = typeof(CheckingFlags)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => f.Name)
            .ToHashSet(StringComparer.Ordinal);

        var props = EngineCheckingFlags();
        Assert.NotEmpty(props);

        // `XxxChecking` on the engine ⇔ `Xxx` in the struct. A flag added as an auto-property would have no
        // struct field and would silently escape PUSH ALL.
        var orphans = props.Select(p => p.Name[..^"Checking".Length])
                           .Where(n => !fields.Contains(n))
                           .ToList();
        Assert.True(orphans.Count == 0,
            $"ExceptionEngine flags with no CheckingFlags field — they would ESCAPE the GR14 PUSH ALL: "
            + string.Join(", ", orphans));
    }

    [Fact]   // PUSH ALL clears every flag and returns the prior state; POP ALL restores every flag exactly.
    public void PushAllCheckingOff_ClearsEveryFlag_AndPopRestoresThem()
    {
        var engine = new ExceptionEngine();
        var props = EngineCheckingFlags();

        foreach (var p in props) p.SetValue(engine, true);

        var saved = engine.PushAllCheckingOff();
        foreach (var p in props)
            Assert.False((bool)p.GetValue(engine)!,
                $"{p.Name} survived PUSH ALL / TURN OFF ALL — GR14 requires every flag off inside a handler body");

        engine.RestoreChecking(saved);
        foreach (var p in props)
            Assert.True((bool)p.GetValue(engine)!,
                $"{p.Name} was not restored by POP ALL — GR14 restores the state taken at the end of imp-1");
    }

    [Fact]   // A flag set to false before PUSH ALL stays false after POP ALL — POP restores, it does not enable.
    public void RestoreChecking_RestoresFalseFlagsAsFalse()
    {
        var engine = new ExceptionEngine { BoundRefModChecking = true, ArgumentFunctionChecking = false };
        var saved = engine.PushAllCheckingOff();
        engine.RestoreChecking(saved);
        Assert.True(engine.BoundRefModChecking);
        Assert.False(engine.ArgumentFunctionChecking);
    }

    /// <summary>kb/Work PB2745 — the AMBIENT STATEMENT CONTEXT has the flags' scope because it is part of the same value:
    /// a checking baseline (an activation, a USE procedure, a PERFORMed range) starts with no context, so a raise there
    /// answers §15.32.3 r1's spaces rather than the activating statement's name, and the restore brings the activating
    /// statement's context back. ISO §15.32.3 r2 (cite.py OK): "the name of the statement that caused the exception
    /// condition to be raised".</summary>
    [Fact]
    public void PushAllCheckingOff_ClearsTheStatementContext_AndRestoreBringsItBack()
    {
        var engine = new ExceptionEngine();
        engine.EnterStatement("CALL", "P; ; 8", ["EC-BOUND-SUBSCRIPT"]);
        var saved = engine.PushAllCheckingOff();
        engine.Set("EC-BOUND-SUBSCRIPT", fatal: false);
        Assert.Null(engine.LastStatement);
        Assert.Null(engine.LastLocation);
        engine.RestoreChecking(saved);
        engine.Set("EC-BOUND-SUBSCRIPT", fatal: false);
        Assert.Equal("CALL", engine.LastStatement);
        Assert.Equal("P; ; 8", engine.LastLocation);
    }
}
