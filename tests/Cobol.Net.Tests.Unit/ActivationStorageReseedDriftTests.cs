// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1132 — <b>every storage channel of automatic data is re-seeded at every activation, by the ONE seed.</b>
/// ISO §8.6.4: local-storage items are "allocated and set to initial state each time the runtime element containing
/// them is activated", and §14.6.2.3.2 action 5 sets "The address of each based item" to null. A cached-singleton
/// program (neither INITIAL nor RECURSIVE) re-enters the SAME instance, so its <c>Call</c> entry re-initializes its
/// LOCAL-STORAGE — and a CELL-BACKED root has no root field: a BASED item's storage is its implicit data-address
/// pointer, an ADDRESS-OF-taken record's is its <c>StorageCell</c>. The field loop skipped both, so the second CALL saw
/// the first activation's address and content (the two-arm shape: the method arm had the re-seed, the program arm did
/// not). Both arms now call <c>OoEmitter.ActivationPointerSeeds</c>.
/// <para>What is measured, over the generated C# (so a channel added to that seed later is covered without editing
/// this test): every per-instance <c>ManagedPointer</c> / <c>StorageCell</c> member the program declares for a
/// LOCAL-STORAGE or non-formal LINKAGE root is ASSIGNED inside <c>Call</c>; the WORKING-STORAGE twin — static data,
/// last-used between activations (§8.6.4) — is NOT; and no such LOCAL-STORAGE member is <c>readonly</c> (a readonly
/// cell could not be re-seeded at all).</para>
/// </summary>
public sealed class ActivationStorageReseedDriftTests
{
    private const string Src = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. ASRSUB.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WB PIC X(3) BASED.
               01 WT PIC X(4) VALUE "WWWW".
               LOCAL-STORAGE SECTION.
               01 LB PIC X(3) BASED.
               01 LA PIC X(4) VALUE "INIT".
               01 LP USAGE POINTER.
               01 LN PIC 9 VALUE 0.
               LINKAGE SECTION.
               01 KB PIC X(3) BASED.
               PROCEDURE DIVISION.
                   SET LP TO ADDRESS OF LA
                   SET ADDRESS OF LB TO ADDRESS OF WT
                   SET ADDRESS OF WB TO ADDRESS OF WT
                   ALLOCATE KB
                   GOBACK.
               END PROGRAM ASRSUB.
        """;

    private static string Emit(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), $"asrdrift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var bound = emitter.Bind(tree!, new EditionContext(2023));
            return emitter.EmitBound(bound, "DRIFTPROBE");
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }

    /// <summary>The emitted <c>Call</c> entry — from its header to the end of its block.</summary>
    private static string CallBody(string cs)
    {
        int at = cs.IndexOf("public void Call(CobolArg[] __args, CobolArg? __ret)", StringComparison.Ordinal);
        Assert.True(at >= 0, "the cached-singleton program emitted no Call entry");
        int end = cs.IndexOf("__asCalled = true;", at, StringComparison.Ordinal);
        Assert.True(end > at, "the Call entry has no activation line");
        return cs[at..end];
    }

    /// <summary>The per-instance pointer/cell member declared for the item <paramref name="cobolName"/>.</summary>
    private static string MemberOf(string cs, string cobolName)
    {
        // `=(?!>)` — the member's DECLARATION, never the BASED deref bridge property (`… _scell_X => CobolPtr.Deref(…)`).
        var m = new Regex($@"^\s*private\s+(?:readonly\s+)?(?:ManagedPointer|StorageCell)\s+(\w*{cobolName}\w*)\s*=(?!>)",
            RegexOptions.Multiline).Match(cs);
        Assert.True(m.Success, $"no pointer/cell member was emitted for {cobolName}");
        return m.Groups[1].Value;
    }

    [Fact]
    public void EveryAutomaticPointerAndCellChannelIsReseededAtTheCallEntry()
    {
        string cs = Emit(Src);
        string call = CallBody(cs);
        foreach (string automatic in new[] { "LB", "LA", "KB" })
        {
            string member = MemberOf(cs, automatic);
            Assert.Matches(new Regex($@"\b{Regex.Escape(member)} = "), call);
            Assert.DoesNotMatch(new Regex($@"private\s+readonly\s+\w+\s+{Regex.Escape(member)}\b"), cs);
        }
        // The WORKING-STORAGE based item is static data: last-used between activations, never re-seeded here.
        Assert.DoesNotMatch(new Regex($@"\b{Regex.Escape(MemberOf(cs, "WB"))} = "), call);
    }
}
