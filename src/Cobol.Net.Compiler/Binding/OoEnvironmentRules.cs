// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>The definition kinds of an object-oriented compilation group that may carry an ENVIRONMENT DIVISION.</summary>
internal enum OoDefinition { Class, Factory, Instance, Interface, Method }

/// <summary>
/// ⛔ THE ONE HOME OF THE OO ENVIRONMENT-DIVISION PLACEMENT RULES (kb/Work PB1076 + PB813). Which parts of an
/// environment division each kind of OO definition may carry is one table, keyed by (definition kind × element),
/// judged once per definition — never a per-site check that covers one arm (before this, only the METHOD arm was
/// screened, and it tested the whole division instead of the sections the standard forbids):
/// <list type="bullet">
/// <item><b>§12.3.3 SR2</b> — a method definition carries no CONFIGURATION SECTION (a bare ENVIRONMENT DIVISION
/// header is legal: §10.6.1 prints <c>[ environment-division ]</c> and §12.2.1 makes both sections optional).</item>
/// <item><b>§12.3.3 SR3</b> — a factory or instance definition carries no SOURCE-COMPUTER, OBJECT-COMPUTER or
/// REPOSITORY paragraph (an accepted one was silently inert).</item>
/// <item><b>§12.4.3 SR1</b> — the INPUT-OUTPUT SECTION belongs to a factory or instance definition only: not a
/// method, not the class definition's own environment division, not an interface.</item>
/// <item><b>§12.3.7.3 SR2 / SR3</b> — SPECIAL-NAMES in a factory or instance definition admits only CURSOR and
/// CRT STATUS; in an interface definition only ALPHABET, CURRENCY, DECIMAL-POINT and LOCALE.</item>
/// </list>
/// The prototype twin (§10.6.2 SR4) is <see cref="PrototypeUnitRules"/>.
/// </summary>
internal static class OoEnvironmentRules
{
    /// <summary>Screen <paramref name="environment"/> (null = none written) of a <paramref name="kind"/> definition.
    /// <paramref name="what"/> names it in the message (<c>method 'M'</c>, <c>class 'C' OBJECT paragraph</c>).</summary>
    internal static void Screen(OoDefinition kind, string what, Core.EnvironmentDivisionContext? environment,
        EditionContext edition)
    {
        if (environment is null) return;

        void Refuse(ParserRuleContext at, string requirement, string rule)
        {
            using var _ = edition.At(at);
            edition.Error(DiagnosticCatalog.OoEnvironmentPlacement, $"{what}: {requirement} (ISO {rule})");
        }

        if (kind == OoDefinition.Method && environment.configurationSection() is { } configuration)
            Refuse(configuration, "a method definition shall not contain a CONFIGURATION SECTION", "§12.3.3 SR2");

        if (kind != OoDefinition.Factory && kind != OoDefinition.Instance
            && environment.inputOutputSection() is { } io)
            Refuse(io, kind switch
            {
                OoDefinition.Method => "a method definition shall not contain an INPUT-OUTPUT SECTION",
                OoDefinition.Interface => "an interface definition shall not contain an INPUT-OUTPUT SECTION",
                _ => "the INPUT-OUTPUT SECTION may be specified only in a factory definition or an instance "
                    + "definition, not in the class definition's own environment division",
            }, "§12.4.3 SR1");

        if (kind is not (OoDefinition.Factory or OoDefinition.Instance or OoDefinition.Interface)) return;
        foreach (var paragraph in environment.configurationSection()?.configurationParagraph() ?? [])
        {
            if (kind != OoDefinition.Interface)
            {
                // §12.3.3 SR3 — names the paragraph, whichever of the three it is.
                ParserRuleContext? forbidden = (ParserRuleContext?)paragraph.sourceComputerParagraph()
                    ?? (ParserRuleContext?)paragraph.objectComputerParagraph()
                    ?? paragraph.repositoryParagraph();
                if (forbidden is not null)
                    Refuse(forbidden, "a factory or instance definition shall not contain a SOURCE-COMPUTER, "
                        + "OBJECT-COMPUTER or REPOSITORY paragraph (the class definition's own environment "
                        + "division declares them)", "§12.3.3 SR3");
            }
            foreach (var entry in paragraph.specialNamesParagraph()?.specialNameEntry() ?? [])
                if (!AdmittedInSpecialNames(kind, entry))
                    Refuse(entry, kind == OoDefinition.Interface
                        ? "the only SPECIAL-NAMES clauses an interface definition may specify are ALPHABET, "
                            + "CURRENCY, DECIMAL-POINT and LOCALE"
                        : "the only SPECIAL-NAMES clauses a factory or instance definition may specify are "
                            + "CURSOR and CRT STATUS", "§12.3.7.3 " + (kind == OoDefinition.Interface ? "SR3" : "SR2"));
        }
    }

    /// <summary>An unrecognized clause is refused BY NAME elsewhere (ClosedFormatPass, COBOLNET1970), so it is not
    /// reported twice here.</summary>
    private static bool AdmittedInSpecialNames(OoDefinition kind, Core.SpecialNameEntryContext e) =>
        e.unrecognizedClause() is not null
        || (kind == OoDefinition.Interface
            ? e.alphabetClause() is not null || e.currencySignClause() is not null
                || e.decimalPointClause() is not null || e.localeClause() is not null
            : e.cursorClause() is not null || e.crtStatusClause() is not null);
}
