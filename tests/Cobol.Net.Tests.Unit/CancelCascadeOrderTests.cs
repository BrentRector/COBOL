// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB234 residue 2 — ISO §14.9.5.4 GR4, RUN-verified rather than derivation-verified: "When a
/// CANCEL statement is executed, all programs contained within the program referenced by the CANCEL statement are
/// also canceled. The result is the same as if an explicit CANCEL statement were executed for each contained
/// program in the reverse order in which the programs appear in the outermost program."
/// <para>The ORDER among sibling containees has no COBOL-observable witness a golden can print — GR9's implicit
/// closes run without USE procedures, and the §14.6.2.3.2 resets are assignments — so the order is observed at
/// the one runtime surface that performs it: <see cref="ProgramTable"/>'s cancel cascade, through each node's
/// GR9 close hook and its static-reset hook, on a real run unit whose programs were really CALLed (GR7 makes a
/// never-called program a no-op, so every node is activated first).</para></summary>
public sealed class CancelCascadeOrderTests
{
    private sealed class Probe(string name, List<string> log) : ICobolProgram
    {
        public void Call(CobolArg[] args, CobolArg? returning) { }
        public void Activate() { }
        public void CloseFiles() => log.Add("close " + name);
    }

    [Fact]
    public void Cancel_CascadesOverContainees_InReverseSourceOrder_BeforeTheContainer()
    {
        var log = new List<string>();
        var table = new RunUnit().Programs;
        // Source order: OUTER contains A, then B (which contains B1), then C — containers register first.
        void Reg(string path, string name, string? parent) =>
            table.Register(path, name, parent, initial: false, common: false, recursive: false,
                _ => new Probe(name, log), staticReset: () => log.Add("reset " + name));
        // One module's registration set, as its __CobolModule.EnsureRegistered() makes it (kb/Work PB2097).
        table.RegisterModule("Cobol.OUTER.__CobolModule", RuntimeAbi.Version.ToString(), RuntimeAbi.CallAbi, () =>
        {
            Reg("OUTER", "OUTER", null);
            Reg("OUTER/A", "A", "OUTER");
            Reg("OUTER/B", "B", "OUTER");
            Reg("OUTER/B/B1", "B1", "OUTER/B");
            Reg("OUTER/C", "C", "OUTER");
        });
        table.CallProgram("OUTER", "", [], null);
        foreach (var n in new[] { "A", "B", "C" }) table.CallProgram(n, "OUTER", [], null);
        table.CallProgram("B1", "OUTER/B", [], null);
        log.Clear();   // Register's §14.6.2.3.2 case-1 resets are not part of the cascade

        table.Cancel("OUTER", "");

        // Reverse source order C, B (itself cascading over B1 first — the same rule one level down), A; each
        // program's own GR9 close then its GR3 initial-state reset; the container last.
        Assert.Equal(new[]
        {
            "close C", "reset C",
            "close B1", "reset B1", "close B", "reset B",
            "close A", "reset A",
            "close OUTER", "reset OUTER",
        }, log);
    }
}
