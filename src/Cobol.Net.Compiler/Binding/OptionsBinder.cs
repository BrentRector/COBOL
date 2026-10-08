// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// Builds the <see cref="OptionsModel"/> from a source unit's OPTIONS paragraph (ISO/IEC 1989:2023 §11.9). The
/// paragraph is now structurally parsed (<c>optionsClause+</c>), so each clause maps to a model field with a typed
/// accessor — no token scanning. An absent paragraph (or clause) leaves the ISO-implied default.
/// </summary>
internal static class OptionsBinder
{
    /// <summary>Read the program unit's OPTIONS paragraph into an <see cref="OptionsModel"/>. The OPTIONS
    /// paragraph was introduced by ISO/IEC 1989:2002 (§11.9 — with the ARITHMETIC clause; Annex E.2 item 21
    /// back-derives the container from obsolete-in-2014 Standard Arithmetic): targeting 85 REJECTS it with an
    /// edition diagnostic and the implied defaults (native arithmetic, nearest-away-from-zero bare ROUNDED)
    /// apply. The 2014-only clauses (DEFAULT ROUNDED, INTERMEDIATE ROUNDING, ENTRY-CONVENTION, FLOAT-BINARY/
    /// -DECIMAL, INITIALIZE; the STANDARD-BINARY/-DECIMAL keywords) carry per-clause 2014 rows in the pass
    /// (P10 Step 12) — a strict 2002 compile fails on those diagnostics before the bound model matters.</summary>
    /// <param name="baseline">The model the unit's clauses override — a contained program's CONTAINER model
    /// (ISO §11.9.4 GR1: the container's clauses apply "unless overridden by a clause in an OPTIONS paragraph in
    /// a contained source element"), null for an outermost unit (the all-defaults model).</param>
    public static OptionsModel Bind(Core.ProgramUnitContext program, EditionContext? edition = null, OptionsModel? baseline = null)
    {
        var start = baseline ?? OptionsModel.Default;
        var paragraphs = program.identificationDivision()?.identificationBody()?.identificationParagraph();
        if (paragraphs is null) return start;

        var options = paragraphs.Select(p => p.optionsParagraph()).FirstOrDefault(o => o is not null);
        return BindParagraph(options, edition, start);
    }

    /// <summary>The paragraph-level core (kb/Work PB135): the OO units carry their OPTIONS paragraph on the
    /// SKELETON (§10.6.1), not inside an identification body, so OoDriver / OoBindMethodData hand the
    /// context in directly — with the container's model as the §11.9.4 GR1 baseline.</summary>
    public static OptionsModel BindParagraph(Core.OptionsParagraphContext? options, EditionContext? edition, OptionsModel? baseline)
    {
        var start = baseline ?? OptionsModel.Default;
        if (options is null) return start;

        // options-paragraph-2002: the pass owns the edition gate (Exec Step E); below 2002 the paragraph is
        // routed INERT (the baseline) — the strict compile fails on the pass's diagnostic before the model matters.
        if (edition is { DialectLevel: < 2002 })
            return start;

        // The clauses fold in written order; a clause written twice or out of the §11.9.2 sequence is refused by
        // ClosedFormatPass from the ClauseCardinalities row (kb/Work PB1508), so a compile that reaches the model
        // has each clause at most once.
        var model = start;
        foreach (var clause in options.optionsClause())
            model = Apply(model, clause, edition);
        return model;
    }

    private static OptionsModel Apply(OptionsModel m, Core.OptionsClauseContext c, EditionContext? edition)
    {
        if (c.arithmeticClause()?.arithmeticMethod() is { } am)
            return m with { Arithmetic = ArithmeticOf(am, edition) };
        if (c.defaultRoundedClause()?.roundingModeName() is { } dr)
            return m with { DefaultRounding = RoundingModes.Map(dr) };
        if (c.entryConventionClause() is { } ec)
            return m with { EntryConvention = EntryConventionOf(ec, edition) };
        if (c.floatBinaryClause()?.endiannessPhrase() is { } fb)
            return m with { FloatBinaryEndianness = FloatFormatPhrase.Endianness(fb) };
        if (c.floatDecimalClause() is { } fdc)
            return DeclineFloatDecimal(m, fdc, edition);
        if (c.intermediateRoundingClause()?.intermediateRoundingMode() is { } ir)
            return m with { IntermediateRounding = RoundingModes.MapIntermediate(ir) };
        if (c.optionsInitializeClause() is { } init)
            return m with { Initialize = InitializeOf(init, edition) };
        return m;
    }

    /// <summary>The ENTRY-CONVENTION clause (ISO §11.9.7) — screened HERE, at the single construction point every
    /// options-bearing production reaches (the <see cref="ArithmeticOf"/> precedent), so the clause is read by
    /// something other than the edition gate (kb/Work PB232).
    ///
    /// <para>§11.9.7.3 SR1 — "The ENTRY-CONVENTION clause may be specified only in a class definition, a function
    /// definition, a function-prototype definition, an interface definition, a program prototype definition, or a
    /// program definition that is not contained within another program." The clause's source element is read off
    /// the parse tree: a contained program, a method definition, and a class's FACTORY / OBJECT paragraph are the
    /// options-bearing elements the list leaves out.</para>
    ///
    /// <para>§11.9.7.4 GR3 — "When entry-convention-name-1 is specified, the meaning of the entry convention is
    /// implementor-defined." This implementation defines NO entry-convention-name (docs/CONFORMANCE.md §7, Annex A.1
    /// item 64): every runtime element is activated through the one COBOL convention, <c>ICobolProgram.Call</c>
    /// with the §8.3.2.2 name mapping (GR2). A name written anyway is refused by name rather than silently activated
    /// with the COBOL convention — a silently-ignored calling convention is a clean compile and a wrong ABI at the
    /// boundary.</para></summary>
    private static string EntryConventionOf(Core.EntryConventionClauseContext ec, EditionContext? edition)
    {
        string name = ec.entryConventionName().GetText();
        if (edition is null) return name;
        using var _ = edition.At(ec);
        if (EntryConventionForbiddenIn(ec) is { } element)
            edition.Error(DiagnosticCatalog.EntryConventionViolation, $"the ENTRY-CONVENTION clause is specified in "
                + $"{element} — it may be specified only in a class, function, function-prototype, interface or "
                + "program-prototype definition, or a program definition that is not contained within another "
                + "program (ISO §11.9.7.3 SR1)");
        if (!CobolNames.Same(name, "COBOL"))
            edition.Error(DiagnosticCatalog.EntryConventionViolation, $"ENTRY-CONVENTION IS {name}: this "
                + "implementation defines no entry-convention-name — the meaning of one is implementor-defined "
                + "(ISO §11.9.7.4 GR3) and the only convention provided is COBOL (Annex A.1 item 64)");
        return name;
    }

    /// <summary>The source element §11.9.7.3 SR1 leaves out that <paramref name="ec"/> is written in, or null when
    /// the clause is where SR1 permits it. The NEAREST enclosing element decides: a method's OPTIONS paragraph sits
    /// inside a class definition, and it is the method that is the clause's source element.</summary>
    private static string? EntryConventionForbiddenIn(Core.EntryConventionClauseContext ec)
    {
        for (Antlr4.Runtime.RuleContext? n = ec.Parent; n is not null; n = n.Parent)
            switch (n)
            {
                case Core.MethodDefinitionContext: return "a method definition";
                case Core.FactoryParagraphContext: return "a factory definition";
                case Core.ObjectParagraphContext: return "an object definition";
                case Core.NestedProgramContext: return "a program contained within another program";
                case Core.ProgramUnitContext or Core.ClassDefinitionContext or Core.InterfaceDefinitionContext:
                    return null;
            }
        return null;
    }

    /// <summary>The ARITHMETIC clause's mode — AND the one place WiseOwl COBOL declines STANDARD-BINARY.
    ///
    /// <para>⛔ THE SCREEN LIVES HERE BECAUSE THIS IS THE SINGLE CONSTRUCTION POINT. It used to live in
    /// <c>DataBinder</c>, which binds programs, functions and the class/factory/object skeletons but NOT a
    /// method's or an interface's OPTIONS paragraph — so <c>METHOD-ID … OPTIONS. ARITHMETIC IS STANDARD-BINARY.</c>
    /// compiled with NO diagnostic at all at <c>--std 2014</c> (measured 2026-08-31, kb/Work PB197), and the
    /// INTERFACE-ID arm was hollow the same way. Six grammar productions carry <c>optionsParagraph?</c>
    /// (identificationParagraph · classDefinition · factoryParagraph · objectParagraph · interfaceDefinition ·
    /// methodDefinition) and every one of them reaches THIS method, so screening the clause where it is READ
    /// makes the next options-bearing production automatic instead of adding a seventh place to remember.
    /// <c>ArithmeticModeScreenDriftTests</c> enumerates the productions out of the grammar and fails when one
    /// is added without a covering fixture.</para>
    ///
    /// <para>The decline is a §4.2.6 processor-dependence choice, not an obsolescence one: A.3 item 2 makes the
    /// clause processor-dependent ("The ARITHMETIC IS STANDARD-BINARY clause in the OPTIONS paragraph is
    /// dependent on the capabilities of the processor"), and §4.2.6 grants the discretion to decline plus the
    /// obligation to warn and to document. Obsolescence grants nothing — F.2 says a conforming implementation
    /// "shall support obsolete language elements EXCEPT for elements that are also optional or
    /// processor-dependent", so it is the processor-dependence that carries this, and F.2 item 3's
    /// reevaluation note is why the decline is recorded as REVISITABLE. docs/CONFORMANCE.md §2 row 2.</para>
    ///
    /// <para>Screening the CLAUSE rather than the resulting model also keeps §11.9.4 GR1 inheritance quiet: a
    /// contained unit that merely inherits the container's mode has written nothing, and is not diagnosed twice.</para>
    /// </summary>
    private static ArithmeticMode ArithmeticOf(Core.ArithmeticMethodContext m, EditionContext? edition)
    {
        if (m.STANDARD_BINARY() is not null)
        {
            edition?.Error("COBOLNET0806", "ARITHMETIC IS STANDARD-BINARY is a processor-dependent language "
                + "element (ISO Annex A.3 item 2) for which support is not claimed (§4.2.6); it is also obsolete "
                + "(§8.8.1.4.1 NOTE 1 / Annex F.2 item 3). Use NATIVE or STANDARD-DECIMAL");
            return ArithmeticMode.StandardBinary;
        }
        return m.STANDARD_DECIMAL() is not null ? ArithmeticMode.StandardDecimal
            : m.STANDARD() is not null ? ArithmeticMode.Standard
            : ArithmeticMode.Native;
    }

    /// <summary>The FLOAT-DECIMAL clause (ISO §11.9.9) — DECLINED, accepted inert with the §4.2.6 ¶3 warning
    /// (COBOLNET2424, kb/Work R43). Annex A.3 item 13 makes the clause dependent "both on the capabilities of the
    /// processor and on support for the standard decimal floating-point usages", and FLOAT-DECIMAL-16/-34 are not
    /// provided (A.3 item 19, refused COBOLNET1564 in <c>PictureAnalyzer.ParseUsage</c>). Every rule the clause
    /// states implies a phrase for "any data item described with a standard decimal floating-point usage"
    /// (§11.9.9.3 SR1-SR6), and no such item can exist, so the model records NOTHING: the two fields this method
    /// used to fill were read by nobody (kb/Work PB298), which is what an inert clause's model looks like when it
    /// is honest about it.</summary>
    private static OptionsModel DeclineFloatDecimal(OptionsModel m, Core.FloatDecimalClauseContext clause,
        EditionContext? edition)
    {
        if (edition is null) return m;
        using var _ = edition.At(clause);
        edition.Declined(DiagnosticCatalog.FloatDecimalClauseUnsupported);
        return m;
    }

    private static OptionsInitialize InitializeOf(Core.OptionsInitializeClauseContext init, EditionContext? edition)
    {
        var target = init.optionsInitializeTarget();
        OptionsSections sections = target.ALL() is not null ? OptionsSections.All : SectionsOf(target, edition);

        var fill = init.optionsInitializeFill();
        // §8.8.3.3 GR3: a concatenation-expression fill literal folds to its equivalent literal's raw text
        // (GetText would glue the operand tokens). OPTIONS precedes SPECIAL-NAMES, so no PCS table applies.
        if (fill.literal() is { } lit)
        {
            string raw = lit.nonNumericLiteral()?.concatenationExpression() is { } ce && edition is not null
                ? ConcatFolder.Fold(ce, edition, LiteralEnvironment.Unscoped).RawText
                : lit.GetText();
            char? c = OneByteHexAlphanumeric(raw);
            if (c is null)
                edition?.Error(DiagnosticCatalog.OptionsInitializeFillLiteral, $"the INITIALIZE clause's fill "
                    + $"literal {raw} is not a one-byte hexadecimal-alphanumeric literal — literal-1 shall be "
                    + "written in the format-2 alphanumeric form X\"nn\" (or X'nn'), exactly two hexadecimal "
                    + "digits (ISO §11.9.10.3 SR1; §8.3.3.2.2 format 2; §8.3.3.2.3 SR5)"
                    // A figurative constant reaches literal-1 only when it is NOT one of the clause's four fill
                    // keywords as printed — the singular spellings (kb/Work PB510): name the keyword it missed.
                    + (lit.nonNumericLiteral()?.figurativeConstant() is not null
                        ? "; the clause's figurative fills are the keywords BINARY ZEROES, HIGH-VALUES, LOW-VALUES "
                          + "and SPACES, each in that one spelling (ISO §11.9.10.2)"
                        : ""));
            // Recovery leaves the byte UNRESOLVED (null), never a guessed one off the rejected literal:
            // InitialStateBackground then falls back to the §11.9.10.4 GR6 no-clause background, so a rejected
            // clause behaves as though it were absent instead of laying down a character the program did not
            // conformingly ask for.
            return new OptionsInitialize(sections, OptionsFill.Literal, raw, c);
        }
        OptionsFill kind =
            fill.BINARY() is not null ? OptionsFill.BinaryZeroes
            : fill.HIGH_VALUES() is not null ? OptionsFill.HighValues
            : fill.LOW_VALUES() is not null ? OptionsFill.LowValues
            : OptionsFill.Spaces;
        // ⛔ No figurative character is resolved here — see OptionsInitialize.LiteralFillChar. GR5 b/d depend on
        // the PROGRAM COLLATING SEQUENCE (§8.3.3.6.4 GR6), which this compiler defines in exactly one place.
        return new OptionsInitialize(sections, kind, null, null);
    }

    /// <summary>ISO §11.9.10.3 SR1's decode: literal-1's byte, or null when the literal is not a ONE-BYTE
    /// HEXADECIMAL-ALPHANUMERIC literal.
    ///
    /// <para>⛔ "Hexadecimal-alphanumeric literal" is a DEFINED TERM and this predicate is written to it, not to
    /// a paraphrase. §8.3.3.2.2 gives the alphanumeric literal exactly two general formats — format 1
    /// (<c>"…"</c> / <c>'…'</c>) and format 2 (<c>X"…"</c> / <c>X'…'</c>) — and "hexadecimal-alphanumeric" names
    /// FORMAT 2. A format-1 literal such as <c>"Z"</c> is therefore NOT admitted however short it is, which is
    /// the one place a natural reading of SR1 ("surely a one-character quoted literal is fine") diverges from
    /// what it says. §8.3.3.2.3 SR5 requires the hex-character-sequence to be composed of hexadecimal digits,
    /// and ONE BYTE is exactly two of them.</para></summary>
    private static char? OneByteHexAlphanumeric(string? raw)
    {
        if (raw is null || raw.Length != 5) return null;                       // X + quote + 2 digits + quote
        if (raw[0] is not ('X' or 'x')) return null;
        if (raw[1] is not ('"' or '\'') || raw[4] != raw[1]) return null;
        return int.TryParse(raw.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out int b) ? (char)b : null;
    }

    /// <summary>The storage-section targets of the INITIALIZE clause. ISO §11.9.10.2 encloses
    /// <c>LOCAL-STORAGE | SCREEN | WORKING-STORAGE</c> in CHOICE INDICATORS inside braces, so §5.2.6.4 admits "any
    /// single alternative … only once" (in any order): the grammar's <c>optionsInitializeSection+</c> is the
    /// any-order half and each alternative's once-only half is read by the ONE reader,
    /// <see cref="ChoiceIndicators.AtMostOnce{T}"/> (COBOLNET2104; kb/Work PB1508 — a bitwise OR folded
    /// <c>WORKING-STORAGE WORKING-STORAGE</c> into one target in silence). The first occurrence of each is bound.</summary>
    private static OptionsSections SectionsOf(Core.OptionsInitializeTargetContext target, EditionContext? edition)
    {
        var sections = OptionsSections.None;
        foreach (var alternative in target.optionsInitializeSection().GroupBy(SectionWord))
        {
            var occurrences = alternative.ToArray();
            Core.OptionsInitializeSectionContext first = occurrences[0];
            if (edition is not null)
            {
                using var _ = edition.At(occurrences.Length > 1 ? occurrences[1] : first);   // the repeat, as ClosedFormatPass reports
                first = ChoiceIndicators.AtMostOnce(edition, occurrences, "the INITIALIZE clause of the OPTIONS paragraph",
                    alternative.Key, "11.9.10.2")!;
            }
            sections |= SectionOf(first, edition);
        }
        return sections;
    }

    /// <summary>The alternative a storage-section target is, as the figure spells it.</summary>
    private static string SectionWord(Core.OptionsInitializeSectionContext s)
        => s.LOCAL_STORAGE() is not null ? "LOCAL-STORAGE" : s.SCREEN() is not null ? "SCREEN" : "WORKING-STORAGE";

    /// <summary>One target of the OPTIONS INITIALIZE clause (ISO §11.9.10.2).
    /// <para>⛔ THE EXPLICIT <c>SCREEN</c> LEG IS REFUSED; THE <c>ALL</c> LEG IS NOT — and the standard itself
    /// split them, which is what makes the distinction legitimate rather than convenient. §11.9.10.4 GR3 ("If
    /// SCREEN is specified, all data items in the screen section are initialized…") is solely conditioned on
    /// the declined Annex A.4.2 module and is refused by name; GR1 ("If ALL is specified, LOCAL-STORAGE, SCREEN,
    /// and WORKING-STORAGE apply") names three targets of which TWO are supported, so <c>INITIALIZE ALL SECTION
    /// TO SPACES</c> stays fully legal — refusing it would reject legal source over a section the program does
    /// not have. Before kb/Work PB260 the SCREEN leg parsed, set this flag, and was read by nothing: a declined
    /// facility a program could write and get a clean compile out of.</para></summary>
    private static OptionsSections SectionOf(Core.OptionsInitializeSectionContext s, EditionContext? edition)
    {
        if (s.LOCAL_STORAGE() is not null) return OptionsSections.LocalStorage;
        if (s.SCREEN() is null) return OptionsSections.WorkingStorage;
        if (edition is not null) ScreenFacility.ReportOptionsInitializeScreen(edition, s);
        return OptionsSections.Screen;
    }
}
