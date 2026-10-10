// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Security.Cryptography;
using System.Text;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO VERSIONS A COMPILED MODULE IS REFUSED BY ARE PINNED BESIDE WHAT THEY GUARD (kb/Work PB2097;
/// docs/rearchitecture/DESIGN-external-repository.md §15.4, §17.2 "Boundary layout"). Every module records the runtime
/// version and the call ABI it was compiled against, and a run unit refuses a module whose runtime MAJOR or call ABI
/// differs (<see cref="RuntimeAbi.Skew"/>). That refusal is only as good as the bumps: a change on either side of the
/// activation boundary that keeps both numbers strands an old module that loads, runs, and reads its arguments wrongly.
/// So each number is pinned beside a hash of what it guards, and the test is red when the hash moves without the number:
/// <list type="bullet">
///   <item><b>The runtime MAJOR</b> (<see cref="RuntimeAbi.Version"/>, <c>Directory.Build.props &lt;Version&gt;</c>)
///     beside the shipped public surface, <c>src/Cobol.Net.Runtime/PublicAPI.Shipped.txt</c>: a new member goes in
///     <c>PublicAPI.Unshipped.txt</c> and moves nothing here; a removed or changed member edits the Shipped file, which is
///     an incompatible change and raises the major. The runtime codecs' images of sample values per byte form ride the
///     same pin: a codec that lays out a value differently is an incompatible change too.</item>
///   <item><b>The call ABI</b> (<see cref="RuntimeAbi.CallAbi"/>) beside the boundary-layout fixture: one caller and one
///     callee whose formals cover each byte form, sign arrangement, width class and crossing arm a Format 1 CALL can
///     pass, compiled by THIS compiler; the hash reads the boundary's compiler-side decisions in the emitted code — each
///     argument's <c>CobolArg</c> (the crossing arm, carrier type and description it carries), the callee's formal
///     adapters (<c>FormalCrossing</c>, <c>FormalCarrierType</c>), the registered formals and RETURNING item
///     (<c>RegisteredFormal</c> / <c>RegisteredReturning</c>) and every profile and group atom array they name.</item>
/// </list>
/// When a hash moves: decide whether the boundary really changed. If it did, raise the number that guards it (a
/// compiler-side crossing change → <see cref="RuntimeAbi.CallAbi"/>; a surface removal or a codec change → the major),
/// then re-pin the hash printed in the failure. If the change is a pure rename with no effect on what crosses (a
/// temporary's name), re-pin alone and say why in the commit.
/// </summary>
public sealed class RuntimeAbiPinDriftTests
{
    // ── The pins. Change a hash only with the decision the class summary describes. ──
    private const int PinnedMajor = 2;
    private const string PinnedRuntimeHash = "515C5AA3212AD0FB";
    private const int PinnedCallAbi = 1;
    private const string PinnedBoundaryHash = "304F0F7F986F680F";

    /// <summary>The boundary-layout fixture: every elementary category and byte form a Format 1 CALL passes BY
    /// REFERENCE and BY CONTENT, a group and a table, and a RETURNING item, with EC-PROGRAM-ARG-MISMATCH checking on
    /// so the callee registers each formal's whole description.</summary>
    private const string Fixture = """
        >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ABICALLER.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-X   PIC X(5) VALUE "ABCDE".
        01 W-N   PIC N(3) VALUE N"XYZ".
        01 W-D   PIC 9(4) VALUE 1234.
        01 W-DS  PIC S9(4) VALUE -1234.
        01 W-DL  PIC S9(4) SIGN LEADING SEPARATE VALUE -1234.
        01 W-DT  PIC S9(4) SIGN TRAILING SEPARATE VALUE -1234.
        01 W-DO  PIC S9(4) SIGN LEADING VALUE -1234.
        01 W-P   PIC S9(5)V99 PACKED-DECIMAL VALUE -12345.67.
        01 W-B   PIC S9(9) BINARY VALUE -123456789.
        01 W-BU  PIC 9(4) COMP-5 VALUE 4321.
        01 W-BL  BINARY-LONG VALUE -42.
        01 W-BD  BINARY-DOUBLE UNSIGNED VALUE 42.
        01 W-F   FLOAT-LONG VALUE 1.5.
        01 W-FS  FLOAT-SHORT VALUE 2.5.
        01 W-E   PIC Z,ZZ9.99 VALUE 1234.5.
        01 W-AE  PIC XXBXX VALUE "ABCD".
        01 W-BO  PIC 1(4) VALUE B"1010".
        01 W-G.
           05 W-GA PIC X(2) VALUE "GH".
           05 W-GB PIC 9(3) VALUE 7.
        01 W-T.
           05 W-TE PIC 9(2) OCCURS 3 VALUE 5.
        01 W-R   PIC S9(4) VALUE 0.
        PROCEDURE DIVISION.
            CALL "ABICALLEE" USING BY REFERENCE W-X W-N W-D W-DS W-DL W-DT W-DO W-P W-B W-BU W-BL W-BD
                W-F W-FS W-E W-AE W-BO W-G W-T
                RETURNING W-R
            CALL "ABICALLEE" USING BY CONTENT W-X W-N W-D W-DS W-DL W-DT W-DO W-P W-B W-BU W-BL W-BD
                W-F W-FS W-E W-AE W-BO W-G W-T
                RETURNING W-R
            STOP RUN.
        END PROGRAM ABICALLER.
        >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ABICALLEE.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X   PIC X(5).
        01 L-N   PIC N(3).
        01 L-D   PIC 9(4).
        01 L-DS  PIC S9(4).
        01 L-DL  PIC S9(4) SIGN LEADING SEPARATE.
        01 L-DT  PIC S9(4) SIGN TRAILING SEPARATE.
        01 L-DO  PIC S9(4) SIGN LEADING.
        01 L-P   PIC S9(5)V99 PACKED-DECIMAL.
        01 L-B   PIC S9(9) BINARY.
        01 L-BU  PIC 9(4) COMP-5.
        01 L-BL  BINARY-LONG.
        01 L-BD  BINARY-DOUBLE UNSIGNED.
        01 L-F   FLOAT-LONG.
        01 L-FS  FLOAT-SHORT.
        01 L-E   PIC Z,ZZ9.99.
        01 L-AE  PIC XXBXX.
        01 L-BO  PIC 1(4).
        01 L-G.
           05 L-GA PIC X(2).
           05 L-GB PIC 9(3).
        01 L-T.
           05 L-TE PIC 9(2) OCCURS 3.
        01 L-R   PIC S9(4).
        PROCEDURE DIVISION USING L-X L-N L-D L-DS L-DL L-DT L-DO L-P L-B L-BU L-BL L-BD
            L-F L-FS L-E L-AE L-BO L-G L-T RETURNING L-R.
            MOVE 1 TO L-R
            GOBACK.
        END PROGRAM ABICALLEE.
        """;

    [Fact]
    public void TheRuntimeMajor_IsPinnedBesideTheShippedSurfaceAndTheCodecImages()
    {
        string hash = Hash(RuntimeHalf());
        Assert.True(RuntimeAbi.Version.Major == PinnedMajor && hash == PinnedRuntimeHash,
            $"the runtime's major is {RuntimeAbi.Version.Major} (pinned {PinnedMajor}) and its shipped surface and codec "
            + $"images hash to {hash} (pinned {PinnedRuntimeHash}). A removed or changed public member, or a codec that lays "
            + "a value out differently, is an incompatible change: raise the major in Directory.Build.props <Version>, then "
            + "pin both here (RuntimeAbiPinDriftTests summary; DESIGN-external-repository §15.4).");
    }

    [Fact]
    public void TheCallAbi_IsPinnedBesideTheBoundaryLayout()
    {
        string hash = Hash(BoundaryHalf());
        Assert.True(RuntimeAbi.CallAbi == PinnedCallAbi && hash == PinnedBoundaryHash,
            $"the call ABI is {RuntimeAbi.CallAbi} (pinned {PinnedCallAbi}) and the boundary-layout fixture's compiler-side "
            + $"decisions hash to {hash} (pinned {PinnedBoundaryHash}). A changed crossing arm, carrier type, registered "
            + "profile or width makes a new caller and an old callee disagree: raise RuntimeAbi.CallAbi, then pin both here "
            + "(RuntimeAbiPinDriftTests summary; DESIGN-external-repository §15.4).");
    }

    [Fact]
    public void TheEmittedRecord_StatesTheVersionsThisRuntimeDeclares()
    {
        string cs = EmitFixture();
        Assert.Contains($"(1, \"{RuntimeAbi.Version}\", {RuntimeAbi.CallAbi}, Registrar = \"Cobol.ABIFIXTURE.__CobolModule\")]", cs, StringComparison.Ordinal);
        Assert.Contains($"RegisterModule(\"Cobol.ABIFIXTURE.__CobolModule\", \"{RuntimeAbi.Version}\", {RuntimeAbi.CallAbi}, Register)", cs, StringComparison.Ordinal);
    }

    /// <summary>The runtime half: the shipped public surface, and the codecs' image of sample values for every byte
    /// form, sign arrangement and binary width.</summary>
    private static IEnumerable<string> RuntimeHalf()
    {
        foreach (string line in File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "PublicAPI.Shipped.txt")))
            yield return line.TrimEnd();
        Int128[] samples = [0, 7, -7, 1234, -1234];
        foreach (var encoding in Enum.GetValues<SignEncoding>())
            foreach (var sign in Enum.GetValues<NumericSign>().Where(s => s != NumericSign.BinaryMinus))
                foreach (var v in samples)
                    yield return Image(new NumProfile { Digits = 4, FractionDigits = 1, Signed = true, SignKind = sign, SignEncoding = encoding,
                        Truncation = NumericTruncation.DigitCount, ByteForm = NumericByteForm.Zoned }, v);
        foreach (var v in samples)
            yield return Image(new NumProfile { Digits = 4, FractionDigits = 0, Signed = false, Truncation = NumericTruncation.DigitCount,
                ByteForm = NumericByteForm.Zoned }, Int128.Abs(v));
        foreach (int width in new[] { 1, 2, 4, 8, 16 })
            foreach (bool signed in new[] { true, false })
                foreach (var v in samples)
                    yield return Image(new NumProfile { Digits = 2 * width, FractionDigits = 0, Signed = signed,
                        SignKind = signed ? NumericSign.BinaryMinus : NumericSign.TrailingOverpunch, Truncation = NumericTruncation.DigitCount,
                        ByteForm = NumericByteForm.Binary, StorageLength = width }, signed ? v : Int128.Abs(v));
        foreach (var form in new[] { NumericByteForm.Packed, NumericByteForm.PackedNoSign })
            foreach (var v in samples)
                yield return Image(new NumProfile { Digits = 5, FractionDigits = 2, Signed = form == NumericByteForm.Packed,
                    Truncation = NumericTruncation.PackedDecimal, ByteForm = form, StorageLength = 3 },
                    form == NumericByteForm.Packed ? v : Int128.Abs(v));
        yield return "X " + Hex(CobolString.Store("ABC", 5));
    }

    private static string Image(NumProfile p, Int128 v) => $"{p} {v} {Hex(CobolNum.FormatImage(v, p))}";

    private static string Hex(string image) => string.Join(" ", image.Select(c => ((int)c).ToString("X4")));

    /// <summary>The compiler half: the boundary decisions in the fixture's emitted C#, read through the Roslyn syntax
    /// tree as tokens (comments and layout are not part of the boundary).</summary>
    private static IEnumerable<string> BoundaryHalf()
    {
        var root = CSharpSyntaxTree.ParseText(EmitFixture()).GetRoot();
        // Each CALL's argument array and RETURNING carrier: the crossing arm, carrier type and description per argument.
        foreach (var call in root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                     .Where(i => i.Expression.ToString() is "ProgramRegistry.CallProgram" or "ProgramRegistry.Register"))
            yield return Tokens(call);
        // The callee's formal adapters: how each formal is carried and landed (FormalCrossing / FormalCarrierType).
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                     .Where(m => m.Identifier.Text == "Call" && m.ParameterList.Parameters.Count == 2))
            yield return Tokens(method);
        // Every numeric profile the two programs declare: the registered and adopted profiles the lines above name.
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>()
                     .Where(f => f.Declaration.Type.ToString() == "NumProfile"))
            yield return Tokens(field);
        // Every §8.5.1.12 atom array the arguments and formal adapters above name by field (GroupAtomTable writes each
        // distinct array once into __GroupAtoms, kb/Work PB2690): the shape of a group or table that crosses, which the
        // lines above now carry only as a reference.
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>()
                     .Where(f => f.Declaration.Type.ToString() == "GroupAtom[]"))
            yield return Tokens(field);
    }

    private static string Tokens(SyntaxNode node) => string.Join(" ", node.DescendantTokens().Select(t => t.Text));

    private static string EmitFixture()
    {
        string path = Path.Combine(Path.GetTempPath(), $"abipin_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, Fixture);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var bound = emitter.Bind(tree!, new EditionContext(2023), frontend.Directives);
            return emitter.EmitBound(bound, "ABIFIXTURE");
        }
        finally { try { File.Delete(path); } catch (IOException) { } }
    }

    private static string Hash(IEnumerable<string> lines) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", lines))))[..16];
}
