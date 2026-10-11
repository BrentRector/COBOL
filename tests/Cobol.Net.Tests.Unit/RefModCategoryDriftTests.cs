// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ REFERENCE MODIFICATION PRESERVES THE CATEGORY (ISO §8.4.3.3.4 GR6) — fix-queue PB20.
/// <para>
/// The rule reads: "The unique data item has the same class, category, and usage as that defined for
/// identifier-1, except that: a) the category alphanumeric-edited is considered class and category alphanumeric,
/// b) the category national-edited is considered class and category national, c) the categories numeric and
/// numeric-edited are considered class and category national if the usage is national; otherwise they are
/// considered class and category alphanumeric."
/// </para>
/// <para>
/// It was implemented THREE TIMES and none was right — <c>IntrinsicArgumentRules.ClassOfPlace</c> returned
/// alphanumeric unconditionally, <c>ExpressionBinder</c> said the same in prose, and <c>MoveBinder</c> carried a
/// partial map that preserved national and boolean but still flattened a national-usage numeric item. **All three
/// cited "ISO §8.4.2.4", a clause that does not exist** — 21 occurrences across 14 files, which is CLAUDE.md
/// rule 1's inherited-citation failure at scale.
/// </para>
/// <para>
/// These are table-driven over the rule itself rather than through a compiled program on purpose: the effect is
/// currently LATENT (no rule that the §15.3 screen consults today demands class boolean, so the corrected
/// categories change no accepted/rejected verdict yet) and it exists to UNBLOCK PB19's INTEGER-OF-BOOLEAN row.
/// A behavioural golden would therefore pin nothing; the rule is a pure function, so it is tested as one.
/// </para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RefModCategoryDriftTests
{
    private static PicInfo Pic(PicCategory cat, Usage usage = Usage.Display) =>
        new(cat, usage, Length: 8, Digits: 0, Scale: 0, Signed: false);

    /// <summary>GR6 BASE — the category is PRESERVED. These are the arms the three old copies did not have, and
    /// boolean is the one that blocks PB19: a ref-modified boolean item is class BOOLEAN, which is why a naive
    /// screen over the old always-alphanumeric answer would have rejected the standard's own Annex D example
    /// <c>FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6))</c>.</summary>
    [Theory]
    [InlineData(PicCategory.Alphanumeric)]
    [InlineData(PicCategory.National)]
    [InlineData(PicCategory.Boolean)]
    public void Gr6Base_PreservesTheCategory(PicCategory cat) =>
        Assert.Equal(cat, RefModPlace.CategoryOf(Pic(cat)));

    /// <summary>GR6 c — numeric and numeric-edited are rewritten, and WHICH way depends on the usage.</summary>
    [Theory]
    [InlineData(PicCategory.Numeric, Usage.Display, PicCategory.Alphanumeric)]
    [InlineData(PicCategory.NumericEdited, Usage.Display, PicCategory.Alphanumeric)]
    [InlineData(PicCategory.Numeric, Usage.National, PicCategory.National)]
    [InlineData(PicCategory.NumericEdited, Usage.National, PicCategory.National)]
    public void Gr6c_RewritesNumericByUsage(PicCategory cat, Usage usage, PicCategory expected) =>
        Assert.Equal(expected, RefModPlace.CategoryOf(Pic(cat, usage)));

    /// <summary>⛔ NO REF-MOD RESULT IS EVER CATEGORY NUMERIC — GR6c rewrites numeric away in both directions.
    /// This is what makes the §8.8.1.1 bar on a reference-modified ARITHMETIC operand correct as it stands, so
    /// PB20 deliberately did NOT touch that screen's verdict (only its citation). Asserted rather than assumed,
    /// because "the arithmetic bar is still right" is exactly the kind of claim that rots silently.</summary>
    [Theory]
    [InlineData(PicCategory.Alphanumeric)]
    [InlineData(PicCategory.Numeric)]
    [InlineData(PicCategory.NumericEdited)]
    [InlineData(PicCategory.National)]
    [InlineData(PicCategory.Boolean)]
    public void NoRefModResult_IsEverNumeric(PicCategory cat)
    {
        Assert.NotEqual(PicCategory.Numeric, RefModPlace.CategoryOf(Pic(cat, Usage.Display)));
        Assert.NotEqual(PicCategory.Numeric, RefModPlace.CategoryOf(Pic(cat, Usage.National)));
    }

    /// <summary>
    /// ⛔ THE CITATION GUARD. "§8.4.2.4" does not exist — `cite.py --check` reports no such clause, §8.4.2 has
    /// only .1/.2/.3, and reference modification is §8.4.3.3. It had been inherited into 21 sites across 14
    /// files, and every reader saw a § and stopped. No runtime test can see a wrong clause NUMBER, so this is a
    /// source-form guard.
    /// <para>⚠ It must not fire on §8.8.4.2.4 (Comparison of numeric operands), which is REAL and appears 15
    /// times — hence the leading boundary. That near-collision is why a naive grep-and-replace on this codebase
    /// is dangerous, and why the check is pinned here rather than left to eyes.</para>
    /// </summary>
    [Fact]
    public void TheNonexistentClause_IsCitedNowhere()
    {
        var offenders = new List<string>();
        foreach (string f in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains(Path.DirectorySeparatorChar + "Generated" + Path.DirectorySeparatorChar)
                || f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                || f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;
            string text = File.ReadAllText(f);
            // (?<![.0-9]) keeps §8.8.4.2.4 — a real clause — from matching.
            foreach (Match m in Regex.Matches(text, @"(?<![.0-9])8\.4\.2\.4(?![0-9])"))
            {
                // The deliberate references that NAME the bad citation in order to warn about it are exempt:
                // they are quoted, and the line says so.
                int ls = text.LastIndexOf('\n', m.Index) + 1;
                int le = text.IndexOf('\n', m.Index); if (le < 0) le = text.Length;
                string line = text[ls..le];
                if (line.Contains("DOES NOT EXIST", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("does not exist", StringComparison.Ordinal)
                    || line.Contains("cite.py --check 8.4.2.4", StringComparison.Ordinal)
                    || line.Contains("there is no clause", StringComparison.Ordinal)) continue;
                offenders.Add($"{Path.GetFileName(f)}: {line.Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "the nonexistent clause §8.4.2.4 is cited again (reference modification is §8.4.3.3.4 GR6):\n  "
            + string.Join("\n  ", offenders));
    }
}
