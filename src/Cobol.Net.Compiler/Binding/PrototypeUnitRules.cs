// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE ONE HOME OF THE PROTOTYPE SOURCE-UNIT RULES (kb/Work PB894). ISO §10.6 writes the rules once for all
/// three prototype kinds — "The following restrictions apply to program prototypes, function prototypes, and
/// method prototypes" (§10.6.2 SR4) — so they are screened once here and every prototype-bearing construct asks:
/// <list type="bullet">
/// <item><b>§10.6.2 SR4 a)–f)</b> — the BODY of a prototype (<see cref="ScreenBody"/>, COBOLNET2272). Called by
/// <c>BinderDriver.MakeUnit</c> for a PROGRAM-ID or FUNCTION-ID prototype and by <c>OoClassTable</c> for an
/// interface's method prototypes. Before PB894 only the method arm had any of it — two of the six restrictions,
/// the data division checked against three of its five non-linkage sections — and a FUNCTION prototype carrying
/// WORKING-STORAGE or procedure statements compiled clean and the statements silently vanished (a prototype emits
/// no body).</item>
/// <item><b>§10.6.1's source-unit formats</b> are NOT here any more: every unit kind — the two prototype kinds and
/// the program and function definitions — is screened by the ONE table in <see cref="SourceUnitShape"/>
/// (COBOLNET2274; kb/Work PB1507).</item>
/// <item><b>§10.6.2 SR1</b> — prototypes precede every other source unit of the compilation group
/// (<see cref="ScreenOrder"/>, COBOLNET2273).</item>
/// </list>
/// §10.6.2 SR2 / SR3 (the prototype-and-definition signature pair) live beside the tables that pair them,
/// <c>BinderDriver.CheckPrototypeSignaturePairs</c>, because they need the bound LINKAGE descriptions.
/// </summary>
internal static class PrototypeUnitRules
{
    /// <summary>§10.6.2 SR4 a)–f) over the four parts a prototype may carry. <paramref name="what"/> names the
    /// prototype in the message (<c>PROGRAM-ID 'P' IS PROTOTYPE</c>, <c>interface 'I', method 'M'</c>).</summary>
    internal static void ScreenBody(string what, Core.OptionsParagraphContext? options,
        Core.EnvironmentDivisionContext? environment, Core.DataDivisionContext? data,
        Core.ProcedureDivisionContext? procedure, EditionContext edition)
    {
        void Refuse(ParserRuleContext at, string rule, string requirement)
        {
            using var _ = edition.At(at);
            edition.Error(DiagnosticCatalog.PrototypeBody,
                $"{what}: {requirement} (ISO §10.6.2 SR4 {rule})");
        }

        // a) "The identification division shall not contain an ARITHMETIC clause."
        foreach (var clause in options?.optionsClause() ?? [])
            if (clause.arithmeticClause() is { } arithmetic)
                Refuse(arithmetic, "a)", "a prototype's identification division shall not contain an ARITHMETIC clause");

        if (environment?.configurationSection() is { } configuration)
            foreach (var paragraph in configuration.configurationParagraph())
            {
                // b) "The environment division shall not contain an object-computer paragraph."
                if (paragraph.objectComputerParagraph() is { } objectComputer)
                    Refuse(objectComputer, "b)", "a prototype's environment division shall not contain an "
                        + "OBJECT-COMPUTER paragraph");
                // c) "The only clauses that may be specified in the SPECIAL-NAMES paragraph are the ALPHABET
                //    clause, the CURRENCY clause, the DECIMAL-POINT clause, the LOCALE clause, and the
                //    SYMBOLIC-CHARACTERS clause." An unrecognized clause is refused BY NAME elsewhere
                //    (ClosedFormatPass, COBOLNET1970), so it is not reported twice here.
                foreach (var entry in paragraph.specialNamesParagraph()?.specialNameEntry() ?? [])
                    if (!AdmittedInPrototype(entry))
                        Refuse(entry, "c)", "the only clauses a prototype's SPECIAL-NAMES paragraph may specify "
                            + "are ALPHABET, CURRENCY, DECIMAL-POINT, LOCALE and SYMBOLIC CHARACTERS");
            }
        // d) "The environment division shall not contain an input-output section."
        if (environment?.inputOutputSection() is { } io)
            Refuse(io, "d)", "a prototype's environment division shall not contain an INPUT-OUTPUT SECTION");

        // e) "The data division may contain only a linkage section."
        if (data is not null)
            foreach (ParserRuleContext? section in (ParserRuleContext?[])[data.fileSection(),
                         data.workingStorageSection(), data.localStorageSection(), data.reportSection(),
                         data.screenSection()])
                if (section is not null)
                    Refuse(section, "e)", "a prototype's data division may contain only a LINKAGE SECTION");

        // f) "The procedure division shall contain only a procedure division header."
        if (procedure is not null
            && (procedure.declarativePart().Length > 0 || procedure.sentence().Length > 0
                || procedure.procedureUnit().Length > 0))
            Refuse(procedure, "f)", "a prototype's procedure division shall contain only a procedure division "
                + "header — no declaratives, sections, paragraphs or statements");
    }

    private static bool AdmittedInPrototype(Core.SpecialNameEntryContext e) =>
        e.alphabetClause() is not null || e.currencySignClause() is not null || e.decimalPointClause() is not null
        || e.localeClause() is not null || e.symbolicCharactersClause() is not null
        || e.unrecognizedClause() is not null;

    /// <summary>§10.6.2 SR1: "Within a compilation group, function-prototypes and program-prototypes shall precede
    /// all other types of source units." Walked over the group's children in source order, so a class or
    /// interface definition counts as an "other type" exactly like a program or function definition.</summary>
    internal static void ScreenOrder(Core.CompilationGroupContext group, EditionContext edition)
    {
        bool sawOther = false;
        foreach (var child in group.children ?? [])
        {
            if (child is Core.ProgramUnitContext unit
                && unit.identificationDivision()?.identificationBody() is { } id
                && (id.programIdParagraph()?.prototypePhrase() ?? id.functionIdParagraph()?.prototypePhrase()) is { } tail)
            {
                if (!sawOther) continue;
                using var _ = edition.At(tail);
                edition.Error(DiagnosticCatalog.PrototypeOrder,
                    "a program or function prototype follows another kind of source unit: within a compilation "
                    + "group, function-prototypes and program-prototypes shall precede all other types of source "
                    + "units (ISO §10.6.2 SR1)");
            }
            else if (child is ParserRuleContext)
                sawOther = true;
        }
    }
}
