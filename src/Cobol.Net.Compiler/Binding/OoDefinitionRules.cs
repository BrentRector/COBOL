// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>The sections of a DATA DIVISION (ISO §13.2), the columns of <see cref="OoDefinitionRules"/>'s table.</summary>
internal enum OoDataSection { File, WorkingStorage, LocalStorage, Linkage, Report, Screen }

/// <summary>
/// ⛔ THE ONE HOME OF THE OO DATA-DIVISION PLACEMENT RULES (kb/Work PB1251; the DATA-division twin of
/// <see cref="OoEnvironmentRules"/>). Which sections of a data division each kind of class-body definition may
/// carry is one table, keyed by (definition kind × section), judged once per parsed definition — before this, only
/// the METHOD arm existed (an ad-hoc block in <c>DataBinder.OoBindMethodData</c>) and a LINKAGE or LOCAL-STORAGE
/// SECTION written in a FACTORY or OBJECT paragraph bound silently as persistent data:
/// <list type="bullet">
/// <item><b>§13.4.3 SR1 / §13.5.3 SR1 / §13.8.3 SR1 / §13.9.3 SR1</b> — within a class definition the file,
/// working-storage, report and screen sections are admitted "only in a factory definition or an instance
/// definition, but not in a method definition".</item>
/// <item><b>§13.6.3 SR1</b> — the local-storage section is admitted in "a program definition or function
/// definition or in a method definition contained in a class definition" — so not in a factory or instance
/// definition.</item>
/// <item><b>§13.7.3 SR1</b> — the linkage section is admitted in "a program definition, function definition,
/// method definition, program prototype definition, or function prototype definition" — so not in a factory or
/// instance definition.</item>
/// </list>
/// A method PROTOTYPE of an interface is a method definition (§10.6.1 NOTE), so it is asked as a METHOD; its own
/// further restriction (the data division "may contain only a linkage section", §10.6.2 SR4 e)) is
/// <see cref="PrototypeUnitRules"/>. The class definition and the interface definition have no data division of
/// their own in the grammar, so they carry no row. Every (kind × section) cell has a verdict — admitted, or the
/// clause that refuses it — and <c>OoDefinitionRulesDriftTests</c> holds the table total.
/// </summary>
internal static class OoDefinitionRules
{
    /// <summary>The definition kinds that carry a data division of their own in a class body.</summary>
    internal static readonly IReadOnlyList<OoDefinition> DataBearingKinds =
        [OoDefinition.Factory, OoDefinition.Instance, OoDefinition.Method];

    /// <summary>The §13 clause that refuses <paramref name="section"/> in <paramref name="kind"/>, or null when the
    /// standard admits it there. Total over <see cref="DataBearingKinds"/> × <see cref="OoDataSection"/>.</summary>
    internal static string? RefusedBy(OoDefinition kind, OoDataSection section) => (kind, section) switch
    {
        // Factory and instance definitions: the "only in a factory or instance" sections are theirs; the two
        // sections that belong to a function, program or method are not.
        (OoDefinition.Factory or OoDefinition.Instance, OoDataSection.File or OoDataSection.WorkingStorage
            or OoDataSection.Report or OoDataSection.Screen) => null,
        (OoDefinition.Factory or OoDefinition.Instance, OoDataSection.LocalStorage) => "§13.6.3 SR1",
        (OoDefinition.Factory or OoDefinition.Instance, OoDataSection.Linkage) => "§13.7.3 SR1",

        // A method definition: LOCAL-STORAGE and LINKAGE only.
        (OoDefinition.Method, OoDataSection.LocalStorage or OoDataSection.Linkage) => null,
        (OoDefinition.Method, OoDataSection.File) => "§13.4.3 SR1",
        (OoDefinition.Method, OoDataSection.WorkingStorage) => "§13.5.3 SR1",
        (OoDefinition.Method, OoDataSection.Report) => "§13.8.3 SR1",
        (OoDefinition.Method, OoDataSection.Screen) => "§13.9.3 SR1",

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"{kind} carries no data division of its own; add it to {nameof(DataBearingKinds)} with a verdict for every section"),
    };

    private static string SectionName(OoDataSection s) => s switch
    {
        OoDataSection.File => "FILE SECTION",
        OoDataSection.WorkingStorage => "WORKING-STORAGE SECTION",
        OoDataSection.LocalStorage => "LOCAL-STORAGE SECTION",
        OoDataSection.Linkage => "LINKAGE SECTION",
        OoDataSection.Report => "REPORT SECTION",
        _ => "SCREEN SECTION",
    };

    /// <summary>Where the standard DOES admit the section the kind may not carry, for the message.</summary>
    private static string AdmittedIn(OoDataSection s) => s switch
    {
        OoDataSection.LocalStorage => "it may appear only in a program, function or method definition",
        OoDataSection.Linkage => "it may appear only in a program, function, method or prototype definition",
        _ => "it may appear only in a factory or instance definition",
    };

    private static string KindName(OoDefinition kind) => kind switch
    {
        OoDefinition.Method => "a method definition",
        OoDefinition.Factory => "a factory definition",
        _ => "an instance definition",
    };

    /// <summary>Screen <paramref name="data"/> (null = none written) of a <paramref name="kind"/> definition.
    /// <paramref name="what"/> names it in the message (<c>method 'M'</c>, <c>class 'C' OBJECT paragraph</c>).</summary>
    internal static void Screen(OoDefinition kind, string what, Core.DataDivisionContext? data, EditionContext edition)
    {
        if (data is null) return;
        foreach (var (section, context) in SectionsOf(data))
        {
            if (RefusedBy(kind, section) is not { } clause) continue;
            using var _ = edition.At(context);
            edition.Error(DiagnosticCatalog.OoDataDivisionPlacement,
                $"{what}: {KindName(kind)} shall not contain a {SectionName(section)} — {AdmittedIn(section)} "
                + $"(ISO {clause})");
        }
    }

    private static IEnumerable<(OoDataSection Section, ParserRuleContext Context)> SectionsOf(Core.DataDivisionContext data)
    {
        if (data.fileSection() is { } f) yield return (OoDataSection.File, f);
        if (data.workingStorageSection() is { } w) yield return (OoDataSection.WorkingStorage, w);
        if (data.localStorageSection() is { } l) yield return (OoDataSection.LocalStorage, l);
        if (data.linkageSection() is { } k) yield return (OoDataSection.Linkage, k);
        if (data.reportSection() is { } r) yield return (OoDataSection.Report, r);
        if (data.screenSection() is { } s) yield return (OoDataSection.Screen, s);
    }
}
