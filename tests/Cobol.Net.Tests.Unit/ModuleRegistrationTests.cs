// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB2097 — the run unit's ONE module-registration set (docs/rearchitecture/DESIGN-external-repository.md
/// §11.2, §17.3 "One registration set"): <see cref="ProgramTable.RegisterModule"/> is the only writer of a run unit's
/// programs, each module is registered once however it is reached (the emitted <c>Main</c>, the sibling-module probe, a
/// host), and a module carrying an already-registered outermost name, or compiled against another runtime major or call
/// ABI, registers NOTHING. ISO/IEC 1989:2023 §8.3.2.2 2) (cite.py OK): "Within a run unit, all instances of a given name
/// that is externalized to the operating environment shall identify the same kind of entity or item ... when two or more
/// source elements identify something with the same externalized name, they refer to the same instance." A refusal is
/// §14.9.4.4 GR3 b)'s EC-PROGRAM-NOT-FOUND ("If the program cannot be located", cite.py OK).</summary>
public sealed class ModuleRegistrationTests
{
    private sealed class Stub : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) { }
        public void Activate() { }
        public void CloseFiles() { }
    }

    private static readonly string Current = RuntimeAbi.Version.ToString();

    /// <summary>A module registrar body registering one outermost program per name, as the emitted <c>Register()</c>
    /// does; <paramref name="runs"/> counts how often it runs.</summary>
    private static Action Registrar(ProgramTable table, Action? runs, params (string Name, bool Function)[] units) => () =>
    {
        runs?.Invoke();
        foreach (var (name, function) in units)
            table.Register(name, name, null, initial: false, common: false, recursive: false, _ => new Stub(), isFunction: function);
    };

    private static bool Located(ProgramTable table, string name)
    {
        table.EntryOf(name, out bool notFound);
        return !notFound;
    }

    [Fact]
    public void AModule_IsRegisteredOnce_HoweverOftenItIsReached()
    {
        var table = new RunUnit().Programs;
        int runs = 0;
        var body = Registrar(table, () => runs++, ("P2097A", false));
        table.RegisterModule("Cobol.A.__CobolModule", Current, RuntimeAbi.CallAbi, body);
        table.RegisterModule("Cobol.A.__CobolModule", Current, RuntimeAbi.CallAbi, body);   // a host after the probe, say
        Assert.Equal(1, runs);
        Assert.True(Located(table, "P2097A"));
    }

    [Fact]
    public void AModuleOfAnotherRuntimeMajor_IsRefused_AndRegistersNothing()
    {
        var table = new RunUnit().Programs;
        int runs = 0;
        var other = new Version(RuntimeAbi.Version.Major + 1, 0, 0, 0).ToString();
        var e = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.B.__CobolModule", other, RuntimeAbi.CallAbi, Registrar(table, () => runs++, ("P2097B", false))));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", e.EcName);
        Assert.Contains(other, e.Message, StringComparison.Ordinal);
        Assert.Contains("major", e.Message, StringComparison.Ordinal);
        Assert.Equal(0, runs);
        Assert.False(Located(table, "P2097B"));
    }

    [Fact]
    public void AModuleOfAnotherCallAbi_IsRefused_AndRegistersNothing()
    {
        var table = new RunUnit().Programs;
        var e = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.C.__CobolModule", Current, RuntimeAbi.CallAbi + 1, Registrar(table, null, ("P2097C", false))));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", e.EcName);
        Assert.Contains($"call ABI {RuntimeAbi.CallAbi + 1}", e.Message, StringComparison.Ordinal);
        Assert.False(Located(table, "P2097C"));
    }

    [Fact]
    public void AModuleOfAnotherMinorVersion_IsCompatible()
    {
        var table = new RunUnit().Programs;
        var newerMinor = new Version(RuntimeAbi.Version.Major, RuntimeAbi.Version.Minor + 1, 0, 0).ToString();
        table.RegisterModule("Cobol.D.__CobolModule", newerMinor, RuntimeAbi.CallAbi, Registrar(table, null, ("P2097D", false)));
        Assert.True(Located(table, "P2097D"));
    }

    [Theory]
    [InlineData(false)]   // the second module defines the name as a program again
    [InlineData(true)]    // ... or as a function: one externalized name identifies one kind of entity (§8.3.2.2 2)
    public void AModuleCarryingAnAlreadyRegisteredName_IsRefusedWhole_NamingBothModules(bool asFunction)
    {
        var table = new RunUnit().Programs;
        table.RegisterModule("Cobol.FIRST.__CobolModule", Current, RuntimeAbi.CallAbi,
            Registrar(table, null, ("P2097F", false), ("P2097X", false)));
        var e = Assert.Throws<CobolCallException>(() =>
            table.RegisterModule("Cobol.SECOND.__CobolModule", Current, RuntimeAbi.CallAbi,
                Registrar(table, null, ("P2097S", false), ("P2097X", asFunction))));
        Assert.Equal("EC-PROGRAM-NOT-FOUND", e.EcName);
        Assert.Contains("Cobol.FIRST.__CobolModule", e.Message, StringComparison.Ordinal);
        Assert.Contains("Cobol.SECOND.__CobolModule", e.Message, StringComparison.Ordinal);
        Assert.Contains("P2097X", e.Message, StringComparison.Ordinal);
        // Nothing of the refused module is registered: not its own program, and not a second P2097X.
        Assert.False(Located(table, "P2097S"));
        Assert.True(Located(table, "P2097F"));
    }

    [Fact]
    public void AProgram_IsRegisteredOnlyThroughItsModule()
    {
        var table = new RunUnit().Programs;
        var e = Assert.Throws<InvalidOperationException>(() =>
            table.Register("P2097R", "P2097R", null, initial: false, common: false, recursive: false, _ => new Stub()));
        Assert.Contains("RegisterModule", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheVersionSkewRule_IsTheMajorAndTheCallAbi()
    {
        Assert.Null(RuntimeAbi.Skew(Current, RuntimeAbi.CallAbi));
        Assert.NotNull(RuntimeAbi.Skew($"{RuntimeAbi.Version.Major + 1}.0.0.0", RuntimeAbi.CallAbi));
        Assert.NotNull(RuntimeAbi.Skew(Current, RuntimeAbi.CallAbi + 1));
        Assert.NotNull(RuntimeAbi.Skew("not a version", RuntimeAbi.CallAbi));
    }
}
