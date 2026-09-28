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
/// kb/Work PB366 + PB365 — the invariants that keep ISO/IEC 1989:2023 §14.9.49.4 GR14 true as the OO emitter
/// grows: <b>each of the Format-4 USE selector's two passes covers EVERY C# type the backend emits for the name
/// that pass's operand alternative wrote, and the class pass precedes the interface pass.</b>
///
/// <para><b>Why the census facts exist.</b> GR14 a) names both object kinds in ONE clause — the exception object
/// "is a factory object or instance object of object-class-name-1 or of a subclass of object-class-name-1" — but
/// a COBOL class is emitted as TWO DISJOINT C# hierarchies: the instance class (rooted at its base's instance
/// class) and the sibling <c>…__FACTORY</c> singleton (rooted at its base's factory class). The generated
/// <c>__EcObjDispatch</c> tested only the INSTANCE name, so every factory exception object selected NO
/// declarative — silently: nothing ran and nothing said so. One COBOL class-name is a SET of emitted C# types,
/// and <see cref="Compiler.Oo.OoClassSymbol.FactoryOrInstanceCsTypes"/> is the ONE place that says which. The
/// interface alternative has the SAME shape and its own census,
/// <see cref="Compiler.Oo.OoInterfaceSymbol.ImplementedCsTypes"/>, which measures ONE today — a COBOL
/// INTERFACE-ID emits exactly one C# interface, carried by BOTH emitted halves of an implementing class — and
/// is measured here rather than assumed, so an interface that ever grows a second emitted half fails at the
/// census instead of silently narrowing a user's declarative selection.</para>
///
/// <para><b>Why the ORDER fact exists.</b> GR14 is a TWO-PASS rule: every object-class-name-1 entry in source
/// order, and only "otherwise, all of the USE statements in the source element are analyzed again" for the
/// interface-name-1 entries. The fixture writes the INTERFACE declarative FIRST on purpose, so an interleaved
/// single pass — the shape PB365 replaced — puts the interface test first and fails here.</para>
///
/// <para>Every fact is asserted against the GENERATED C# rather than by reading the emitter's source.</para>
/// </summary>
public sealed class Format4UseObjectSelectorDriftTests
{
    private const string Src = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. F4DRIFT.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS F4CBASE
            CLASS F4CSUB
            INTERFACE F4IDRIFT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 U USAGE OBJECT REFERENCE.
        PROCEDURE DIVISION.
        DECLARATIVES.
        IFACE-SEC SECTION.
            USE AFTER EXCEPTION OBJECT F4IDRIFT.
        IFACE-P.
            DISPLAY "IFACE-HANDLER".
        BASE-SEC SECTION.
            USE AFTER EXCEPTION OBJECT F4CBASE.
        BASE-P.
            DISPLAY "BASE-HANDLER".
        END DECLARATIVES.
        MAIN SECTION.
        MAIN-P.
            SET U TO F4CBASE.
            RAISE U.
            STOP RUN.
        END PROGRAM F4DRIFT.

        IDENTIFICATION DIVISION.
        INTERFACE-ID. F4IDRIFT.
        END INTERFACE F4IDRIFT.

        IDENTIFICATION DIVISION.
        CLASS-ID. F4CBASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            INTERFACE F4IDRIFT.
        IDENTIFICATION DIVISION.
        OBJECT.
        IMPLEMENTS F4IDRIFT.
        END OBJECT.
        END CLASS F4CBASE.

        IDENTIFICATION DIVISION.
        CLASS-ID. F4CSUB INHERITS FROM F4CBASE.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS F4CBASE.
        END CLASS F4CSUB.
        """;

    /// <summary>Compile the fixture to C#, paired with the class table the same bind produced.</summary>
    private static (string CSharp, Compiler.Oo.OoClassTable Classes) EmitFixture()
    {
        string path = Path.Combine(Path.GetTempPath(), $"f4drift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, Src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2002 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var comp = emitter.Bind(tree!, new EditionContext(2002));
            return (emitter.EmitBound(comp), comp.OoClasses);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>The generated <c>__EcObjDispatch</c> body — the two GR14 passes and the -3 tail.</summary>
    private static string Selector(string csharp)
    {
        var body = Regex.Match(csharp, @"__EcObjDispatch\(object\?\s+__obj\)\s*\{(?<b>.*?)\n\s*\}",
            RegexOptions.Singleline);
        Assert.True(body.Success, $"no generated __EcObjDispatch in:\n{csharp}");
        return body.Groups["b"].Value;
    }

    /// <summary>Every C# type the backend emits FOR a COBOL class is one this class symbol's census names.
    /// `__` cannot occur in a COBOL-derived name, so `F4CBASE` and `F4CBASE__…` are exactly that class's
    /// emitted halves — the census must equal them, in both directions.</summary>
    [Fact]
    public void EveryEmittedHalfOfAClass_IsInTheClassSymbolsCensus()
    {
        var (csharp, classes) = EmitFixture();
        var cls = classes.Find("F4CBASE");
        Assert.NotNull(cls);

        var emitted = Regex.Matches(csharp, @"\bclass\s+(" + cls!.CsName + @"(?:__[A-Z0-9_]+)?)\b\s*:")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(cls.FactoryOrInstanceCsTypes.ToHashSet(StringComparer.Ordinal), emitted);
    }

    /// <summary>The interface twin of the census fact (kb/Work PB365's alternative, PB366's shape). A COBOL
    /// INTERFACE-ID emits exactly ONE C# interface, which both emitted halves of an implementing class carry —
    /// so GR14 b) needs one type test where GR14 a) needs two. That "one" is MEASURED here, not assumed: a
    /// second emitted half would fail at the census, where it is one line to fix.</summary>
    [Fact]
    public void EveryEmittedHalfOfAnInterface_IsInTheInterfaceSymbolsCensus()
    {
        var (csharp, classes) = EmitFixture();
        var ifc = classes.FindInterface("F4IDRIFT");
        Assert.NotNull(ifc);

        var emitted = Regex.Matches(csharp, @"\binterface\s+(" + ifc!.CsName + @"(?:__[A-Z0-9_]+)?)\b")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ifc.ImplementedCsTypes.ToHashSet(StringComparer.Ordinal), emitted);
    }

    /// <summary>The generated §14.9.49.4 GR14 selector tests each operand's WHOLE census — for a class, the
    /// instance half AND the factory half, so "a factory object or instance object of object-class-name-1"
    /// selects the declarative for either kind (testing one half is the PB366 wrong answer); for an interface,
    /// the emitted interface every implementing half carries (GR14 b), the PB365 alternative).</summary>
    [Fact]
    public void ObjDispatchSelector_TestsEveryHalfOfTheNamedClass()
    {
        var (csharp, classes) = EmitFixture();
        var cls = classes.Find("F4CBASE");
        var ifc = classes.FindInterface("F4IDRIFT");
        Assert.NotNull(cls);
        Assert.NotNull(ifc);
        string predicate = Selector(csharp);

        foreach (string half in cls!.FactoryOrInstanceCsTypes.Concat(ifc!.ImplementedCsTypes))
            Assert.Matches(@"\bis\b[^;]*\b" + Regex.Escape(half) + @"\b", predicate);
    }

    /// <summary>⛔ GR14 IS TWO PASSES, and the generated code shows it: every object-class-name-1 entry is
    /// tested before ANY interface-name-1 entry, because pass b) runs only when "all of the USE statements in
    /// the source element are analyzed again". The fixture writes the INTERFACE declarative FIRST, so a single
    /// interleaved source-order scan — which is what the selector was before kb/Work PB365 — emits the
    /// interface test first and fails here (and at runtime would let an earlier interface entry beat a later
    /// class entry, which GR14 orders the other way round).</summary>
    [Fact]
    public void ObjDispatchSelector_RunsTheClassPassBeforeTheInterfacePass()
    {
        var (csharp, classes) = EmitFixture();
        var cls = classes.Find("F4CBASE");
        var ifc = classes.FindInterface("F4IDRIFT");
        Assert.NotNull(cls);
        Assert.NotNull(ifc);
        string predicate = Selector(csharp);

        int classAt = predicate.IndexOf($"is {cls!.CsName} or", StringComparison.Ordinal);
        int ifaceAt = predicate.IndexOf($"is {ifc!.CsName})", StringComparison.Ordinal);
        Assert.True(classAt >= 0, $"no class-pass test for {cls.CsName} in:\n{predicate}");
        Assert.True(ifaceAt >= 0, $"no interface-pass test for {ifc.CsName} in:\n{predicate}");
        Assert.True(classAt < ifaceAt,
            "the interface-name-1 test precedes the object-class-name-1 test — GR14 a) runs to exhaustion "
            + $"BEFORE the USE statements are analyzed again for b):\n{predicate}");
    }
}
