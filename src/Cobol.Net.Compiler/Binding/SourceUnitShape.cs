// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>The four unit kinds ISO §10.6.1 gives a general format of their own (the class and interface
/// definitions are screened by the OO binder).</summary>
internal enum SourceUnitKind { ProgramDefinition, ProgramPrototype, FunctionDefinition, FunctionPrototype }

/// <summary>
/// ⛔ THE ONE SCREEN OF A SOURCE UNIT'S SHAPE — ISO §10.6.1 (kb/Work PB894 for the prototype kinds, PB1507 for the
/// rest). The grammar parses every PROGRAM-ID / FUNCTION-ID unit with ONE superset rule (<c>programUnit</c> /
/// <c>nestedProgram</c>: any division, <c>nestedProgram*</c>, an optional end marker of either keyword), because
/// ANTLR cannot make the four formats differ by the kind of the paragraph that opens them. What the formats say is
/// therefore read HERE, from <see cref="FormatOf"/>, for EVERY unit kind — until PB1507 only the two prototype kinds
/// were asked, so a function nested in a program, a function without its end marker, <c>END FUNCTION</c> closing a
/// program and a program that contains programs but owns no procedure division all compiled clean.
/// <para>The four formats differ in exactly the dimensions of <see cref="Format"/>: whether the unit can be a
/// contained program-definition, whether it has the <c>[ procedure-division [ program-definition ] … ]</c> slot,
/// which keyword closes it, and whether the end marker is bracketed. A fifth kind, or a fifth dimension, is one row
/// or one column of THAT table.</para></summary>
internal static class SourceUnitShape
{
    /// <summary>One row of §10.6.1: <paramref name="Noun"/> names the kind in a message;
    /// <paramref name="MayBeContained"/> — the kind is a <c>program-definition</c> and so fits another program
    /// definition's contained slot; <paramref name="MayContain"/> — the format prints that slot;
    /// <paramref name="EndsWithProgram"/> — the end marker is <c>END PROGRAM</c> (else <c>END FUNCTION</c>);
    /// <paramref name="EndMarkerRequired"/> — the end marker is printed unbracketed.</summary>
    private sealed record Format(string Noun, bool MayBeContained, bool MayContain, bool EndsWithProgram,
        bool EndMarkerRequired);

    private static Format FormatOf(SourceUnitKind kind) => kind switch
    {
        SourceUnitKind.ProgramDefinition => new("program definition", true, true, true, false),
        SourceUnitKind.ProgramPrototype => new("program prototype", false, false, true, true),
        SourceUnitKind.FunctionDefinition => new("function definition", false, false, false, true),
        SourceUnitKind.FunctionPrototype => new("function prototype", false, false, false, true),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>The §10.6.1 kind of a unit, from the two facts the binder reads off its identification paragraph.</summary>
    internal static SourceUnitKind KindOf(bool isFunction, bool isPrototype) =>
        (isFunction, isPrototype) switch
        {
            (false, false) => SourceUnitKind.ProgramDefinition,
            (false, true) => SourceUnitKind.ProgramPrototype,
            (true, false) => SourceUnitKind.FunctionDefinition,
            (true, true) => SourceUnitKind.FunctionPrototype,
        };

    /// <summary>Screen one unit against its format. <paramref name="what"/> names the unit in the message
    /// (<c>FUNCTION-ID 'F'</c>); <paramref name="container"/> is the kind of the unit that contains it, or null for
    /// a unit of the compilation group itself.</summary>
    internal static void Screen(string what, SourceUnitKind kind, SourceUnitKind? container, Core.ProgramUnitContext unit,
        EditionContext edition)
    {
        var format = FormatOf(kind);

        void Refuse(ParserRuleContext at, string requirement)
        {
            using var _ = edition.At(at);
            edition.Error(DiagnosticCatalog.SourceUnitFormat, $"{what}: {requirement} (ISO §10.6.1)");
        }

        // Positioned on the identification division: a CONTAINED unit's context is the synthetic programUnit
        // BinderDriver.Reparent builds, whose own Start token is null.
        ParserRuleContext at = (ParserRuleContext?)unit.identificationDivision() ?? unit;

        // The unit as a CONTAINEE: only a program-definition fits the contained slot of a program definition.
        if (container is not null && !format.MayBeContained)
            Refuse(at, $"a {format.Noun} is a source unit of the compilation group itself; a program definition "
                + "may contain only program definitions");

        // The unit as a CONTAINER: only a program-definition prints the slot, and the slot is inside the
        // procedure-division bracket — `[ procedure-division [ program-definition ] … ]`.
        var nested = unit.nestedProgram();
        foreach (var contained in nested)
            if (!format.MayContain)
                Refuse(contained, $"a {format.Noun} contains no other source unit — its format has no contained "
                    + "program-definition");
        if (format.MayContain && nested.Length > 0 && unit.procedureDivision() is null)
            Refuse(nested[0], "a program definition's contained programs follow its procedure division in the format "
                + "(`[ procedure-division [ program-definition ] … ]`), so a program that contains programs shall "
                + "have a procedure division");

        // The end marker: required where the format prints it unbracketed, and the keyword is the format's own.
        if (unit.endProgramHeader() is not { } end)
        {
            if (format.EndMarkerRequired)
                Refuse(at, $"the end marker is required — the {format.Noun} format prints "
                    + $"END {(format.EndsWithProgram ? "PROGRAM" : "FUNCTION")} unbracketed");
        }
        else if ((end.PROGRAM() is not null) != format.EndsWithProgram)
            Refuse(end, $"END {(format.EndsWithProgram ? "FUNCTION" : "PROGRAM")} cannot close a {format.Noun} — its "
                + $"format ends with END {(format.EndsWithProgram ? "PROGRAM" : "FUNCTION")}");
    }
}
