// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE DESCRIPTION OF A REPORT ITEM, READ FROM ITS ENTRY, NOT FROM THE REPORT WALK (kb/Work PB1941). An elementary
/// report item is described by the clauses §13.15.4 GR2 makes "the same clauses" as a data description entry's — USAGE,
/// PICTURE, BLANK WHEN ZERO and JUSTIFIED — with its SIGN clause, the VALUE literal §13.15.3 SR14 implies a PICTURE
/// from, and the USAGE written on an enclosing report group description entry (§13.18.60.4 GR1: "If the USAGE clause
/// is specified or implied at a
/// group level, it applies only to each elementary item in the group"). None of these is the walk's state (lines,
/// columns, repetitions, sums), so the description is a function of the written entries alone, decoded ONCE per entry:
/// <list type="bullet">
/// <item>the report walk (<c>BindReportEntry</c>) reads it for every entry it binds, and builds the printable item's
/// picture through <see cref="ReportPrintablePicture"/>;</item>
/// <item>a constant's LENGTH OF / BYTE-LENGTH OF (§13.10.3 SR11: "Data-name-1 and data-name-2, if defined in the report
/// section, shall reference elementary report items") measures the item through the same two functions, WHEREVER the
/// constant is evaluated — in the FILE SECTION before the REPORT SECTION binds, in an earlier report group, or in the
/// very group or entry the walk is binding. §13.10.3 SR4 forbids only a length that depends on the constant, and that
/// cycle is the constant's own (its PICTURE names it).</item>
/// </list>
/// Both read the entry under its own cursor and with the walk's own wording, so a fault in the description is one
/// diagnostic however many times it is read (<c>EditionContext.AddOnce</c>).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The clauses that describe one report group description entry's item, as written and decoded.</summary>
    /// <param name="PicText">The PICTURE character-string, its constant-names expanded (§13.10.3 SR2), or null.</param>
    /// <param name="UsageText">The entry's own USAGE (the last clause written), or null.</param>
    /// <param name="InheritedUsage">The usage written on the nearest enclosing entry that writes one (§13.18.60.4 GR1).</param>
    /// <param name="Editing">The PICTURE EDITING phrases (§13.18.40.2), or null.</param>
    /// <param name="Locale">The PICTURE LOCALE phrase (format 2), or null.</param>
    /// <param name="OwnSign">The SIGN clause, or null.</param>
    /// <param name="Justified">The JUSTIFIED clause is written.</param>
    /// <param name="BlankWhenZero">The BLANK WHEN ZERO clause is written.</param>
    /// <param name="ValueRaws">The VALUE clause's literal operands (§13.18.63.2 format 4), in the order written.</param>
    private sealed record ReportItemDescription(
        string? PicText, string? UsageText, string? InheritedUsage, List<EditingPhraseSpec>? Editing,
        LocaleEditSpec? Locale, SignSpec? OwnSign, bool Justified, bool BlankWhenZero, IReadOnlyList<string> ValueRaws);

    private readonly Dictionary<Core.ReportGroupEntryContext, ReportItemDescription> _reportItemDescriptions = [];

    /// <summary>The description of the entry at <paramref name="at"/>, decoded on first ask (see the type summary).</summary>
    private ReportItemDescription DescribeReportItem(ReportEntryLocation at)
    {
        var ge = at.Entry;
        if (_reportItemDescriptions.TryGetValue(ge, out var known)) return known;
        using var _ = Edition.At(ge);
        string? entryName = ge.dataName().NameOrNull();
        string where = $"RD '{at.ReportName}' entry '{entryName ?? "FILLER"}'";
        string? picText = null, usageText = null;
        List<EditingPhraseSpec>? editing = null;
        LocaleEditSpec? locale = null;
        SignSpec? ownSign = null;
        bool justified = false, blankWhenZero = false;
        var valueRaws = new List<string>();
        foreach (var clause in ge.reportGroupClause())
        {
            if (clause.pictureClause() is { } pc && pc.PIC_STRING() is { } pic)
            {
                picText = pic.GetText();
                // §13.15.4 GR2 imports the PICTURE clause, and §13.10.3 SR2 lets an integer constant-name specify
                // repetition in a picture character-string: expanded before the analyzer reads it, exactly as BindEntry
                // does for a data description entry (kb/Work PB1226's sibling sweep).
                if (UnitHasConstants) picText = ExpandPicConstants(picText, where);
                editing = BuildEditingSpecs(pc, $"report group entry '{entryName ?? "FILLER"}'");
                // PICTURE format 2 in a REPORT GROUP entry (§13.15.4 GR2 imports the PICTURE clause's own rules, so
                // format 2 is LEGAL here; kb/Work PB113 — this arm used to ignore the phrase and analyze the picture as
                // format 1: a silent wrong answer). ⛔ Do NOT pair it with a SIGN check: §13.15.3 carries no twin of
                // §13.16.3 SR19 — the pair is legal here.
                if (pc.pictureLocalePhrase() is { } rlp)
                {
                    var localeName = rlp.cobolWord();   // locale-name-1; LOCALE is the formatWord (kb/Work PB764)
                    var localeRef = LocaleRef.Current;
                    if (localeName is not null
                        && ResolveLocaleName(localeName.GetText(), $"{where} PICTURE … LOCALE {localeName.GetText()}",
                            "ISO §13.18.40.3 SR37 — locale-name-1 shall be specified in the LOCALE clause in the SPECIAL-NAMES paragraph")
                            is { } sym)
                        localeRef = new LocaleRef(sym);
                    int size = Math.Max(1, IntegerOperandValue(rlp.integerOperand(), $"{where} PICTURE … LOCALE SIZE")
                        ?? RecoveredIntegerOperand);
                    locale = new LocaleEditSpec(localeRef, size, "");
                }
            }
            else if (clause.usageClause() is { } usage)
            {
                usageText = UsageKeyword(usage);
                // ⛔ §13.18.60.3 SR7 IS ABOUT THE CLAUSE, NOT ABOUT THE PRINTABLE LEAF (kb/Work PB541): "Only the DISPLAY
                // or NATIONAL phrase may be specified in any USAGE clause associated with a report group item." A USAGE
                // clause on a GROUP entry is associated with the report group items under it (GR1), so it is screened
                // HERE, where every entry's clause passes — before this, a group entry's `USAGE COMP` was captured and
                // then silently discarded, and the only usage screen in the compiler was a staged-loud on the leaf's
                // analyzed picture.
                ScreenReportUsage(PictureAnalyzer.ParseUsage(usageText, Edition, where), at.ReportName, entryName, usageText);
            }
            else if (clause.signClause() is { } sign)
                ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
            else if (clause.justifiedClause() is not null)
                justified = true;
            else if (clause.blankWhenZeroClause() is not null)
                blankWhenZero = true;
            // Format 4 (report-section), ISO §13.18.63.2 — `{literal-1}…`, an operand LIST; the same ONE literal-position
            // reader as Format 1 per operand, so a non-literal operand is reported here too (kb/Work PB732: an undefined
            // word used to be written into the report as its own spelling, exit 0) and the operands are never glued
            // (kb/Work PB506).
            else if (clause.valueClause() is { } value && ExtractValueOperandList(value, where) is { } raws)
                valueRaws.AddRange(raws);
        }
        var described = new ReportItemDescription(picText, usageText, InheritedReportUsage(at), editing, locale, ownSign,
            justified, blankWhenZero, valueRaws);
        // A constant the PICTURE names may have measured this very entry meanwhile (the SR4 cycle, reported there).
        return _reportItemDescriptions.TryAdd(ge, described) ? described : _reportItemDescriptions[ge];
    }

    /// <summary>§13.18.60.4 GR1 — the usage the entry at <paramref name="at"/> inherits: the one written on the nearest
    /// enclosing report group description entry that writes one, or null. The enclosing entries are the ones before it
    /// at a lower level-number, up to its level 1 entry (§13.15's level hierarchy, read as
    /// <see cref="ReportEntryLocation.Qualification"/> reads it).</summary>
    private string? InheritedReportUsage(ReportEntryLocation at)
    {
        int level = ReportEntryLevel(at.Entry);
        for (int j = at.Index - 1; j >= 0 && level > 1; j--)
        {
            int l = ReportEntryLevel(at.Entries[j]);
            if (l >= level) continue;
            var enclosing = DescribeReportItem(at with { Index = j });
            return enclosing.UsageText ?? enclosing.InheritedUsage;
        }
        return null;
    }

    /// <summary>The PICTURE of the item <paramref name="d"/> describes, as the printable item has it (§13.18.14): its
    /// written PICTURE analyzed under its effective usage, sign, editing and locale, or the PICTURE §13.15.3 SR14 implies
    /// from its one VALUE literal; null when it has neither. The report walk builds the printable item from it, and a
    /// constant's length phrase measures the item by it — one analysis, so the two cannot disagree.</summary>
    private PicInfo? ReportPrintablePicture(ReportItemDescription d, string reportName, string? entryName)
    {
        string itemWhere = $"RD '{reportName}' printable item '{entryName ?? "FILLER"}'";
        Usage itemUsage = PictureAnalyzer.ParseUsage(d.UsageText ?? d.InheritedUsage, Edition, itemWhere);
        return d.PicText is not null
            ? PictureAnalyzer.Analyze(d.PicText, itemUsage, Edition, itemWhere, d.OwnSign, currencies: CurrencySigns,
                blankWhenZero: d.BlankWhenZero, editing: d.Editing, localeFormat2: d.Locale,
                decimalPointIsComma: DecimalPointIsComma)
            // ⛔ ISO §13.15.3 SR14 — THE REPORT GROUP ENTRY'S OWN VALUE-IMPLIED PICTURE, word for word the §13.16.3 SR9
            // rule this compiler synthesizes for a data description entry: "The PICTURE clause may be omitted for an
            // elementary item when an alphanumeric, boolean or national literal that is not a zero-length literal is
            // specified in the VALUE clause." ONE classifier for both formats (DataBinder.ImpliedPicture.cs). The
            // CONTEXT §8.3.3.6.4 GR1/GR4 ask about is this entry's own usage — a report group entry is not subject to
            // §13.18.60.4 GR1 inheritance from a data-division group, and §13.18.60.3 SR7 admits only DISPLAY or
            // NATIONAL here anyway, so GR4's boolean context cannot arise.
            // ⛔ THE SINGULAR "the literal": SR14 fixes `length` from THE literal, so the implied PICTURE exists only
            // for a clause that supplies exactly ONE operand. A multi-operand format-4 VALUE clause (§13.18.63.2,
            // kb/Work PB506) implies none — the standard names no rule for which of several literals fixes the ONE
            // description all the repetitions share, and taking the first would silently truncate every longer one
            // (DETERMINATION, kb/Work PB504 × PB506).
            : d.ValueRaws is [{ } value] && Sr9ImpliedFor(value, itemUsage) is { } implied
                ? ImpliedReportPicture(implied, itemUsage, d.UsageText is not null, d.OwnSign, itemWhere)
                : null;
    }
}
