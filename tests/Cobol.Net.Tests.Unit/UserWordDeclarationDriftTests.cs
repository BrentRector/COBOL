// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY TYPE OF USER-DEFINED WORD IS DECLARED THROUGH THE ONE FUNNEL (kb/Work PB1083; ISO §8.3.2.2).
/// <para>§8.3.2.1 rule 5 and §12.3.8.3 SR12/SR13 forbid a user-defined word of ANY type from spelling an
/// intrinsic-function-name the REPOSITORY paragraph identifies. The screen was called from six hand-picked declaration
/// funnels, so an alphabet-name, a class-name, a symbolic-character, a program-name or a function-prototype-name
/// spelling one compiled clean. Every declaration now announces its word through <c>DataBinder.DeclareUserWord</c> with
/// its <see cref="UserWordKind"/>, and this test keeps that total: the enum carries exactly the types §8.3.2.2 lists
/// (parsed from <c>specs/ISO_COBOL.md</c>, never copied by hand), and every type is declared at some call site or
/// carried below as a type this compiler never declares in a bound source element, with the reason.</para>
/// <para>The same funnel asks §8.3.2.2's other two rules about a word AS DECLARED (kb/Work PB990, PB1403): one TYPE per
/// word within a source element, except as the standard's three exceptions say, and a letter in every word but a
/// section-name, paragraph-name or level-number. Both rule tables are DATA (<c>UserWordKinds</c>) rebuilt below from the
/// standard's own text, so an edit to either, or to the standard's wording, cannot drift silently.</para>
/// </summary>
public sealed class UserWordDeclarationDriftTests
{
    /// <summary>The §8.3.2.2 types no binder declares, and why. A type here shall NOT also be declared (the list
    /// would then be stale), and each reason names the rule or the mechanism that makes the exemption true.</summary>
    private static readonly Dictionary<UserWordKind, string> NotDeclaredInASourceElement = new()
    {
        [UserWordKind.CompilationVariableName] = "§8.3.2.1: compilation-variable-names form intersecting sets with "
            + "intrinsic-function-names, and §8.3.2.2 exception 1 — they live in the text-manipulation stage",
        [UserWordKind.LevelNumber] = "a level-number is digits, never an intrinsic-function-name, and declares nothing",
        [UserWordKind.DirectiveName] = "the PUSH/POP directive-name names a compiler directive — a reference processed "
            + "in the text-manipulation stage, not a declaration",
    };

    private static string[] SpecListedTypes()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int start = spec.IndexOf("The types of user-defined words are:", StringComparison.Ordinal);
        Assert.True(start >= 0, "§8.3.2.2's list header moved in specs/ISO_COBOL.md — re-derive this guard");
        int end = spec.IndexOf("Within a source element, a given user-defined word", start, StringComparison.Ordinal);
        Assert.True(end > start, "§8.3.2.2's list end moved in specs/ISO_COBOL.md — re-derive this guard");
        return [.. Regex.Matches(spec[start..end], @"^— ([a-z-]+)\s*$", RegexOptions.Multiline).Select(m => m.Groups[1].Value)];
    }

    /// <summary>§8.3.2.2's text after the type list, up to the sentence that ends the exceptions.</summary>
    private static string ExceptionsText()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int start = spec.IndexOf("Within a source element, a given user-defined word", StringComparison.Ordinal);
        Assert.True(start >= 0, "§8.3.2.2's exceptions moved in specs/ISO_COBOL.md — re-derive this guard");
        int end = spec.IndexOf("Further rules for uniqueness", start, StringComparison.Ordinal);
        Assert.True(end > start, "§8.3.2.2's exceptions end moved in specs/ISO_COBOL.md — re-derive this guard");
        return spec[start..end];
    }

    private static UserWordKind KindOfSpelling(string spelling) =>
        Enum.GetValues<UserWordKind>().Single(k => k.Spelling() == spelling);

    /// <summary>⛔ THE ONE-TYPE-PER-WORD CENSUS IS THE STANDARD'S, NOT OURS (kb/Work PB990): which types may share a
    /// word is written ONCE (<see cref="UserWordKinds.MayBeOneWord"/>) and this test rebuilds the whole 31 x 31 relation
    /// from §8.3.2.2's own exceptions, parsed from <c>specs/ISO_COBOL.md</c>, and fails on the first pair the two
    /// disagree about — so an edit to the sets, or to the standard's text, cannot drift silently.</summary>
    [Fact]
    public void TheSharingRelation_IsRebuiltFromTheStandardsExceptions()
    {
        string text = ExceptionsText();
        Assert.Contains("1\\) a compilation-variable-name may be the same as any other type of user-defined word", text);
        var levelNumberMatch = Regex.Match(text, @"2\\\) a level-number may be the same as a ([a-z-]+) or a ([a-z-]+)");
        Assert.True(levelNumberMatch.Success, "exception 2 moved — re-derive this guard");
        var withLevelNumber = new HashSet<UserWordKind>
            { KindOfSpelling(levelNumberMatch.Groups[1].Value), KindOfSpelling(levelNumberMatch.Groups[2].Value) };
        int three = text.IndexOf("3\\) the same name may be used as any of the following", StringComparison.Ordinal);
        Assert.True(three >= 0, "exception 3 moved — re-derive this guard");
        var oneName = Regex.Matches(text[three..], @"^— ([a-z-]+)\s*$", RegexOptions.Multiline)
            .Select(m => KindOfSpelling(m.Groups[1].Value)).ToHashSet();
        Assert.True(oneName.Count >= 5, $"parsed only {oneName.Count} types from exception 3 — re-derive this guard");

        foreach (var a in Enum.GetValues<UserWordKind>())
            foreach (var b in Enum.GetValues<UserWordKind>())
            {
                bool expected = a == b
                    || a == UserWordKind.CompilationVariableName || b == UserWordKind.CompilationVariableName
                    || (a == UserWordKind.LevelNumber && withLevelNumber.Contains(b))
                    || (b == UserWordKind.LevelNumber && withLevelNumber.Contains(a))
                    || (oneName.Contains(a) && oneName.Contains(b));
                Assert.True(expected == UserWordKinds.MayBeOneWord(a, b),
                    $"§8.3.2.2 says a {a.Spelling()} and a {b.Spelling()} {(expected ? "MAY" : "may NOT")} share a word; "
                    + $"UserWordKinds.MayBeOneWord says the opposite");
            }
    }

    /// <summary>⛔ THE LETTERLESS TYPES ARE THE STANDARD'S (kb/Work PB1403): §8.3.2.2 — "<i>With the exception of
    /// section-names, paragraph-names, and level-numbers, each user-defined word shall contain at least one basic
    /// letter or extended letter</i>". <see cref="UserWordKinds.MayBeLetterless"/> is rebuilt from that sentence.</summary>
    [Fact]
    public void TheLetterlessTypes_AreTheStandardsExceptionList()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        var m = Regex.Match(spec, @"With the exception of ([a-z-]+), ([a-z-]+), and ([a-z-]+), each user-defined word shall contain at least one basic letter");
        Assert.True(m.Success, "§8.3.2.2's letter rule moved in specs/ISO_COBOL.md — re-derive this guard");
        Assert.Equal(
            new[] { m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value }.Select(s => s.TrimEnd('s')).Order(),
            UserWordKinds.MayBeLetterless.Select(k => k.Spelling()).Order());
    }

    /// <summary>The census runs at the ONE declaration funnel and nowhere else: a second caller would be a second
    /// place the rule is asked, and a declaring construct that reached only the second would escape the first.</summary>
    [Fact]
    public void TheCensus_IsAskedOnlyByTheOneDeclarationFunnel()
    {
        var callers = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(f => (File: f, Text: File.ReadAllText(f)))
            .Where(s => Regex.IsMatch(s.Text, @"(?<!void )CheckOneTypePerWord\("))
            .Select(s => Path.GetFileName(s.File)).ToList();
        Assert.Equal(["DataBinder.cs"], callers);
        string binder = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "DataBinder.cs"));
        int declare = binder.IndexOf("internal bool DeclareUserWord(", StringComparison.Ordinal);
        Assert.True(declare >= 0);
        string head = binder[declare..(declare + 260)];
        Assert.Contains("CheckOneTypePerWord(word, kind);", head);
        Assert.Contains("CheckLetter(word, kind);", head);   // §8.3.2.2's letter rule, kb/Work PB1403 — the same funnel
    }

    /// <summary>⛔ A PARAGRAPH-NAME-OMITTED PARAGRAPH IS NAMED NOWHERE (ISO §14.4.3; kb/Work PB990): the procedure table
    /// registered a declarative section's leading sentences, and its empty-section no-op pc, as a paragraph named like
    /// the SECTION — a paragraph the source never wrote, which §8.3.2.2's one-type-per-word census found declared
    /// as both and which made a legal reference to the section ambiguous. They go through the anonymous-paragraph
    /// entry points, so no call of <c>AddParagraph</c> passes a section's name.</summary>
    [Fact]
    public void NoSectionNameIsRegisteredAsAParagraph()
    {
        string builder = File.ReadAllText(Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "Procedure",
            "ProcedureTableBuilder.cs"));
        Assert.DoesNotMatch(new Regex(@"\bAddParagraph\(\s*name\b"), builder);
    }

    /// <summary>The enum is §8.3.2.2's list: one member per listed type, no member the list does not name.</summary>
    [Fact]
    public void TheEnum_IsTheStandardsList()
    {
        var listed = SpecListedTypes();
        Assert.True(listed.Length >= 30, $"parsed only {listed.Length} types from §8.3.2.2 — re-derive this guard");
        Assert.Equal(listed.Order(StringComparer.Ordinal),
            Enum.GetValues<UserWordKind>().Select(k => k.Spelling()).Order(StringComparer.Ordinal));
    }

    /// <summary>Every type is declared somewhere (a <c>UserWordKind.X</c> argument in the compiler — the enum's only
    /// consumer is <c>DataBinder.DeclareUserWord</c>) or exempt with its reason, never both.</summary>
    [Fact]
    public void EveryType_IsDeclared_OrExemptWithItsReason()
    {
        string enumFile = Path.GetFullPath(TestRepo.Src("Cobol.Net.Compiler", "Binding", "UserWordKind.cs"));
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cs in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFullPath(cs) == enumFile || cs.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(cs), @"\bUserWordKind\.(\w+)"))
                used.Add(m.Groups[1].Value);
        }
        Assert.Contains(nameof(UserWordKind.DataName), used);   // the scan found the declarations at all
        foreach (var kind in Enum.GetValues<UserWordKind>())
        {
            bool declared = used.Contains(kind.ToString());
            bool exempt = NotDeclaredInASourceElement.ContainsKey(kind);
            Assert.True(declared ^ exempt, declared
                ? $"{kind.Spelling()} is declared through DataBinder.DeclareUserWord AND listed exempt — drop the exemption"
                : $"no declaration of a {kind.Spelling()} reaches DataBinder.DeclareUserWord — §8.3.2.1 rule 5 / §12.3.8.3 "
                  + "SR12-SR13 do not reach it; call the funnel where the word is declared, or exempt it with the rule "
                  + "that makes the exemption true");
        }
    }
}
