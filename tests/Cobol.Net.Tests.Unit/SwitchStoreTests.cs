// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The external-switch store reads its facility ONCE (kb/Work PB2771). ISO §12.3.7.4 GR4 leaves the scope of each
/// external switch and the facility that modifies it to the implementor; WiseOwl COBOL's documented definition
/// (docs/CONFORMANCE.md DOC-A.1-191, required by §4.2.5) is "the value is read at the first interrogation and a later
/// SET governs for the rest of the run unit". So the first interrogation latches the status whatever the environment
/// held — an ABSENT variable latches OFF exactly as an empty one does — and the environment changing afterwards never
/// changes a switch-status condition (§8.8.4.6). The facility's variable family is the closed switch-name set, not
/// every <c>COBOL_</c>-prefixed word.
/// </summary>
[Collection("process-globals")]   // the tests set and restore PROCESS-global environment variables
public sealed class SwitchStoreTests
{
    /// <summary>Runs <paramref name="body"/> with <paramref name="switchName"/>'s variable set to <paramref name="value"/>
    /// (null = absent), restoring the variable's prior value afterwards.</summary>
    private static void WithVariable(string switchName, string? value, Action<Action<string?>> body)
    {
        string variable = SwitchStore.VariableNameFor(switchName);
        string? prior = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, value);
            body(v => Environment.SetEnvironmentVariable(variable, v));
        }
        finally { Environment.SetEnvironmentVariable(variable, prior); }
    }

    [Fact]
    public void AnAbsentVariable_LatchesOff_AtTheFirstInterrogation()
    {
        WithVariable("SWITCH-30", null, setEnvironment =>
        {
            var store = new SwitchStore();
            Assert.False(store.Get("SWITCH-30"));
            setEnvironment("ON");
            Assert.False(store.Get("SWITCH-30"));   // the host gained COBOL_SWITCH_30; no SET executed: still OFF
            setEnvironment("OFF");
            Assert.False(store.Get("SWITCH-30"));
        });
    }

    [Fact]
    public void AnEmptyVariable_AgreesWithAnAbsentOne()
    {
        WithVariable("SWITCH-31", "", setEnvironment =>
        {
            var store = new SwitchStore();
            Assert.False(store.Get("SWITCH-31"));
            setEnvironment("ON");
            Assert.False(store.Get("SWITCH-31"));
        });
    }

    [Theory]
    [InlineData("ON")]
    [InlineData("on")]
    [InlineData("1")]
    [InlineData("True")]
    public void APresentOnVariable_LatchesOn_AtTheFirstInterrogation(string value)
    {
        WithVariable("SWITCH-32", value, setEnvironment =>
        {
            var store = new SwitchStore();
            Assert.True(store.Get("SWITCH-32"));
            setEnvironment("OFF");
            Assert.True(store.Get("SWITCH-32"));   // read once: a later environment change is not seen
        });
    }

    [Fact]
    public void ALaterSet_GovernsAfterTheLatch_AndTheEnvironmentNeverDoesAgain()
    {
        WithVariable("SWITCH-33", null, setEnvironment =>
        {
            var store = new SwitchStore();
            Assert.False(store.Get("SWITCH-33"));
            store.Set("SWITCH-33", true);
            Assert.True(store.Get("SWITCH-33"));
            setEnvironment("OFF");
            Assert.True(store.Get("SWITCH-33"));
            store.Set("SWITCH-33", false);
            setEnvironment("ON");
            Assert.False(store.Get("SWITCH-33"));
        });
    }

    [Fact]
    public void TheLatchIsPerStore_SoANewRunUnitReadsTheEnvironmentAgain()
    {
        WithVariable("SWITCH-34", null, setEnvironment =>
        {
            Assert.False(new SwitchStore().Get("SWITCH-34"));
            setEnvironment("ON");
            Assert.True(new SwitchStore().Get("SWITCH-34"));   // a new run unit (RunUnit.Switches) is a new store
        });
    }

    [Fact]
    public void TheFacilityFamily_IsTheClosedSwitchNameSet_NotEveryCobolPrefixedWord()
    {
        Assert.Equal(37 + 8, SwitchStore.Names.Count);
        // A family entry names its members: one that declared none would match no name at all.
        foreach (var family in RuntimeConfig.All.Where(e => e.IsPattern)) Assert.NotNull(family.FamilyMember);
        foreach (string name in SwitchStore.Names)
        {
            Assert.True(SwitchStore.IsVariableName(SwitchStore.VariableNameFor(name)), name);
            Assert.NotNull(RuntimeConfig.Find(SwitchStore.VariableNameFor(name)));
        }
        Assert.True(SwitchStore.IsVariableName("COBOL_SWITCH_1"));
        Assert.True(SwitchStore.IsVariableName("COBOL_UPSI_7"));
        foreach (string notAVariable in new[] { "COBOL_TYPO", "COBOL_", "COBOL_SWITCH_37", "COBOL_UPSI_8", "SWITCH_1", "cobol_switch_1" })
        {
            Assert.False(SwitchStore.IsVariableName(notAVariable), notAVariable);
            Assert.Null(RuntimeConfig.Find(notAVariable));   // a variable the runtime never reads is not reported as one
        }
    }

    [Fact]
    public void TheCompilersSwitchNameTable_IsBuiltFromTheStoresOwnNames()
    {
        // DOC-A.1-191: one set, written once (SwitchStore.Names) — the SPECIAL-NAMES table and the facility cannot drift.
        var tableSwitches = ImplementorNames.All.Values.Where(r => r.Kind == SystemNameKind.Switch).Select(r => r.Name);
        Assert.Equal(SwitchStore.Names.OrderBy(n => n, StringComparer.Ordinal), tableSwitches.OrderBy(n => n, StringComparer.Ordinal));
    }
}
