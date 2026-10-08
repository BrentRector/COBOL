// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LEVEL-88 LITERAL IS PLACED BY THE VALUE-CLAUSE RECIPE, NOT BY A RECIPE OF THE SET EMITTER'S OWN
/// (kb/Work PB560) — and this keeps that true.
///
/// <para>ISO §14.9.39.4 GR6 does not describe a store: it says the literal "is placed in the conditional
/// variable according to the rules for the VALUE clause", and GR7 says the same of the FALSE phrase's
/// literal-4. §8.8.4.5.3 GR3 then makes the condition true exactly when the stored value is one of the
/// condition's values, so the STORE and the TEST are required to answer identically — a SET followed by its
/// own IF is an identity. When the rule is written down twice they drift, and they did: the SET emitter's
/// private recipe stored the raw literal text where the VALUE clause composes an edited image (PIC ZZ9.99
/// held `10    `, not ` 10.00`), scaled a float literal at the item's scale of zero (VALUE 0.5 stored 0),
/// ignored BLANK WHEN ZERO, had no runtime compose for a PICTURE format-2 (LOCALE) variable, and handed a
/// <c>long</c> to the <c>string</c> field of a whole-group-aliased numeric item — a Roslyn CS1503 that failed
/// the whole compilation of legal COBOL. Each of those five was a branch
/// <c>ValueInitializer.InitializerFrom</c> already had, because the VALUE clause needs it.</para>
///
/// <para>These are source-shape assertions rather than behaviour assertions on purpose: the behaviour is
/// pinned by the conformance goldens (<c>pb560_condition_value_*</c> at 85 / 2002 / 2023), and what those
/// cannot see is a SIXTH branch being added to a private copy tomorrow.</para>
/// </summary>
public sealed class ConditionValueRecipeDriftTests
{
    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("Generated", StringComparison.Ordinal)) continue;
            yield return (Path.GetRelativePath(root, f), File.ReadAllText(f));
        }
    }

    private static string SetEmitterSource() =>
        CompilerSources().Single(s => s.Rel.EndsWith("SetEmitter.cs", StringComparison.Ordinal)).Text;

    /// <summary>The population guard (feedback_green_gates_arent_evidence): the emitter, the statement it
    /// emits and the recipe it must call all still exist, so the absence assertions below are absences of
    /// something rather than absences of everything.</summary>
    [Fact]
    public void TheSetConditionStore_CallsTheOneValueRecipe()
    {
        string set = SetEmitterSource();
        Assert.Contains("EmitSet(BoundSetConditions", set, StringComparison.Ordinal);
        Assert.Contains("ValueImageOf(", set, StringComparison.Ordinal);
        Assert.Contains(CompilerSources(),
            s => s.Rel.EndsWith("DataEmitter.cs", StringComparison.Ordinal)
              && s.Text.Contains("ValueImageOf(DataItem item, string raw) => _values.InitializerFrom(item, raw)",
                                 StringComparison.Ordinal));
    }

    /// <summary>⛔ THE REGRESSION THAT WAS THE BUG. The SET emitter may not spell any part of the VALUE-clause
    /// composition for itself — the numeric-edited image (§13.18.63.3 SR6), the figurative / ALL fold
    /// (§8.3.3.6.4 GR2), the float approximation (§13.18.63.4 GR17 → GR1), the category stores, the locale
    /// compose (§13.18.40.5 editing rule 11). Every one of them belongs to <c>ValueInitializer.InitializerFrom</c>,
    /// which §14.9.39.4 GR6 names by reference.</summary>
    [Fact]
    public void TheSetEmitter_SpellsNoPartOfTheValueRecipe()
    {
        string set = SetEmitterSource();
        // Comments quote the deleted code by name, so only real code is scanned.
        string code = new Regex(@"^\s*(///|\*|//).*$", RegexOptions.Multiline).Replace(set, "");
        string[] primitives =
        [
            // Only names that belong to the VALUE recipe itself. NumFormatImage is deliberately ABSENT:
            // the index-augment and SET CONTENT arms encode a numeric item's byte window through it for
            // reasons that have nothing to do with a VALUE clause, so listing it would make this assertion
            // fire on code the rule does not govern.
            "StrStore", "StrStoreBoolean", "UnscaledAtScale", "RepeatToWidth", "FigurativeConstants",
            "FigurativeInitializer", "EditedImageOfNumericValue", "LocaleEditCompose", "RawValueAsFloat",
        ];
        var hits = primitives.Where(p => code.Contains(p, StringComparison.Ordinal)).ToList();
        Assert.True(hits.Count == 0,
            "SetEmitter is composing a VALUE again instead of calling DataEmitter.ValueImageOf — ISO "
            + "§14.9.39.4 GR6 places a level-88 literal 'according to the rules for the VALUE clause', and a "
            + "second copy of those rules is how the STORE and the TEST stopped agreeing (kb/Work PB560): "
            + string.Join(", ", hits));
    }

    /// <summary>The numeric-edited VALUE image has exactly THREE readers, and the list is EXHAUSTIVE — a fourth
    /// caller is a fourth copy of §13.18.63.3 SR6 (feedback_one_rule_one_place); the SET store used to be
    /// exactly such a site, spelled differently.
    /// <para>The three are the recipe that composes the image (<c>ValueInitializer</c>), the membership test
    /// that compares against it (<c>ConditionRenderer</c> — §8.8.4.5.3 GR2 compares by the relation-condition
    /// rules, so the store and the test must see the same image), and, since kb/Work PB920, the BINDER's
    /// §13.18.63.3 SR26 / SR27 screens (<c>DataBinder.ConditionValueOf</c>). The third was added
    /// DELIBERATELY and is named here rather than worked around: those two syntax rules compare "<i>the value
    /// of</i>" two VALUE-clause operands, and on a numeric-edited subject that value IS the edited image —
    /// §8.8.4.2.1's NOTE, "<i>All comparisons involving numeric-edited data items are alphanumeric or national
    /// comparisons, including when the associated VALUE clause is a numeric literal</i>". A screen that composed
    /// its own image would be the very drift this test exists to catch.</para></summary>
    [Fact]
    public void TheNumericEditedValueImage_HasExactlyThreeReaders()
    {
        var callers = CompilerSources()
            .Where(s => new Regex(@"EditedImageOfNumericValue\s*\(").IsMatch(s.Text))
            .Select(s => Path.GetFileName(s.Rel))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["ConditionRenderer.cs", "DataBinder.cs", "ValueInitializer.cs"], callers);
    }

    /// <summary>⛔ AND THE COMPOSER TAKES NO EMIT CONTEXT (kb/Work PB920). SR6 is asked during BINDING as
    /// well as during emission, so an <c>EmitContext</c> parameter would have made the binder unable to call it
    /// — which is exactly how a second copy of the rule gets written. The signature carries the facts the rule
    /// needs instead: the dialect level and the item, whose PICTURE carries its own decimal-point mode
    /// (<c>PicInfo.DecimalPointIsComma</c>, kb/Work PB2554 — the mode belongs to the item, not to the unit that
    /// emits it).</summary>
    [Fact]
    public void TheNumericEditedValueImage_IsPhaseNeutral()
    {
        string values = CompilerSources()
            .Single(s => s.Rel.EndsWith("ValueInitializer.cs", StringComparison.Ordinal)).Text;
        Assert.Contains("EditedImageOfNumericValue(int dialectLevel, DataItem item, PicInfo pic, string raw)",
            values, StringComparison.Ordinal);
    }

    /// <summary>The recipe reads the receiver's description through THE ONE category reader
    /// (<c>DataItem.OperandPic</c>, D20) — never raw <c>Pic</c>, which is null for every group and made a
    /// group conditional variable a null-reference the moment the SET store started calling it. §14.9.39.4 GR6
    /// names group conditional variables explicitly, so the group arms are the recipe's own business, and
    /// §13.18.63.3 SR4 is what describes an ORDINARY one (alphanumeric literals, bounded by the size of the
    /// group item).</summary>
    [Fact]
    public void TheValueRecipe_ReadsTheOperandDescription()
    {
        string values = CompilerSources()
            .Single(s => s.Rel.EndsWith("ValueInitializer.cs", StringComparison.Ordinal)).Text;
        Assert.Contains("var pic = item.OperandPic ?? AsIfAlphanumericGroup(item);", values, StringComparison.Ordinal);
        Assert.DoesNotContain("var pic = item.Pic!;", values, StringComparison.Ordinal);
    }
}
