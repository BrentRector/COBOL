// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A DIAGNOSTIC NAMES AN ISO §8.5.2.1 CLASS THROUGH THE ONE SPELLING, NEVER ITS OWN (kb/Work PB1400). The class
/// lattice (<c>CobolClass</c>) carries two REFINED members a CATEGORY-worded rule needs — numeric-edited of usage display
/// and of usage national — and <c>IntrinsicArgumentRules.Name</c> spells them as the class Table 2 gives them
/// ("alphanumeric (numeric-edited)"). The messages that spelled the enum themselves with
/// <c>ToString().ToLowerInvariant()</c> described a numeric-edited subscript, CALL / CANCEL target, BY VALUE argument or
/// report CODE operand as "class numericediteddeediting"; and <c>ExpressionBinder.NonNumericOperandKind</c> wrote the class OUT as a literal —
/// "group item … (class alphanumeric, ISO §8.5)" — for every group, which told the user a GROUP-USAGE BIT group (class
/// boolean, §8.5.2.5 3)) and a GROUP-USAGE NATIONAL group (class national, §8.5.2.10 3)) were alphanumeric.
/// <para>The class of a place is spelled by <c>IntrinsicArgumentRules.ClassNameOf</c> (or <c>Name</c> over a class the
/// caller already holds); this sweep fails on an interpolated class word built from an enum's <c>ToString()</c>, and
/// on the literal group-class text that stood in the arithmetic-operand screen.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ClassSpellingDriftTests
{
    // "class {x.ToString()…}", "{x.ToString()…} class", ClassOf…(…).ToString() bound to a local, and an interpolated
    // class-named local ({cls.ToString()…}) whose "class " sits on the previous source line.
    private static readonly Regex OwnSpelling = new(
        @"class \{[^{}]*\.ToString\(\)|\{[^{}]*\.ToString\(\)[^{}]*\} class|ClassOf\w*\([^()]*\)\??\.ToString\(\)"
        + @"|\{\w*(?:[cC]ls|Class)\w*(?:\.Value)?\??\.ToString\(\)",
        RegexOptions.CultureInvariant);

    [Fact]
    public void NoDiagnostic_SpellsAClassFromTheEnumName()
    {
        string root = TestRepo.Src(["Cobol.Net.Compiler"]);
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            .Where(t => OwnSpelling.IsMatch(t.line))
            .Select(t => $"{Path.GetRelativePath(root, t.f)}:{t.i + 1}: {t.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "spell a class through IntrinsicArgumentRules.Name / ClassNameOf (kb/Work PB1400):\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void TheArithmeticOperandScreen_AsksTheClassItNames()
    {
        string text = File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", "Binding", "Procedure", "ExpressionBinder.cs"]));
        Assert.DoesNotContain("(class alphanumeric, ISO §8.5)", text);
        Assert.DoesNotContain("a reference-modified operand (class alphanumeric", text);
        Assert.Contains("IntrinsicArgumentRules.ClassNameOf(p)", text);
    }

    // The universal claim ISO §8.5.2.1 refutes, in the forms it was written: "a group is class alphanumeric", "A group
    // item (class alphanumeric, …)", "a group IS class alphanumeric", "a group is class and category alphanumeric", "a
    // group is category alphanumeric". Only an ALPHANUMERIC group is; a bit group is boolean, a national group national
    // and a strongly-typed group its type-name. A claim that goes on to list the other classes ("alphanumeric, boolean
    // or national") is the rule stated whole and passes. One source line at a time, so a claim split over a line break
    // escapes it (the one in pb59_operand_category_total.cob was, and was corrected by hand with the rest).
    private static readonly Regex UniversalGroupClass = new(
        @"\b(?:a|any|every)\s+group(?:\s+item)?\s*(?:is\s+|\()?(?:of\s+)?(?:class|category)"
        + @"(?:\s+and\s+(?:class|category))?\s+alphanumeric(?!,\s*(?:boolean|national)|\s+or\b|/)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>The review of kb/Work PB1400 found the refuted rule still stated as fact in the class doc of the test
    /// that pins its correction; the sweep that followed found it in source comments and corpus fixtures across the tree — a
    /// sweep of <c>ExpressionBinder.cs</c> alone could not see them. Every C# source and every corpus program is
    /// scanned.</summary>
    [Fact]
    public void NoSourceOrCorpusComment_SaysEveryGroupIsClassAlphanumeric()
    {
        string self = Path.GetFullPath(TestRepo.Tests(["Cobol.Net.Tests.Unit", "ClassSpellingDriftTests.cs"]));
        static bool Built(string f) =>
            f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
            || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");
        var files = Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(TestRepo.Tests(["conformance"]), "*.cob", SearchOption.AllDirectories))
            .Where(f => !Built(f) && Path.GetFullPath(f) != self);
        var offenders = files
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (f, line, i)))
            .Where(t => UniversalGroupClass.IsMatch(t.line))
            .Select(t => $"{Path.GetRelativePath(TestRepo.Root, t.f)}:{t.i + 1}: {t.line.Trim()}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "ISO §8.5.2.1: only an ALPHANUMERIC group is class alphanumeric — a bit group is boolean, a national group "
            + "national, a strongly-typed group its type-name (kb/Work PB1400):\n" + string.Join("\n", offenders));
    }
}
