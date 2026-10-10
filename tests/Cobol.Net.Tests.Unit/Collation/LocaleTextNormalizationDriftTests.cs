// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Runtime.Globalization;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// ⛔ NO LOCALE-SOURCED STRING REACHES A COBOL RESULT UN-NORMALIZED (DETERMINATION L12; kb/Work PB2764). LOCALE-DATE,
/// LOCALE-TIME and LOCALE-TIME-FROM-SECONDS format only through <see cref="TimeFacts"/> (the LC_TIME snapshot) and
/// LC_MONETARY only through <see cref="MonetaryFacts"/>; the raw <see cref="DateTimeFormatInfo"/> /
/// <see cref="NumberFormatInfo"/> that <see cref="LocaleFacts"/> still publishes carry the host ICU's bytes — U+200F in
/// every ar-* date separator, U+00A0 inside the AM/PM designators of nine es-* / yrl-* cultures, U+202F where an older
/// ICU had a plain space — and a fixed-width COBOL receiver counts character positions. Three guards:
/// (1) the RESULT of every predefined culture holds no format character and no NBSP/narrow/thin space;
/// (2) every unquoted ASCII letter of every culture's d_fmt / t_fmt is a token the renderer handles, so the next ICU
/// release cannot slip an unrendered letter into a result; (3) no product source outside the snapshot classes reads a
/// culture's <c>DateTimeFormat</c> / <c>NumberFormat</c>, so a new caller cannot reintroduce the raw read.
/// </summary>
public sealed class LocaleTextNormalizationDriftTests
{
    private static bool IsHostVarying(char c) =>
        c is ' ' or ' ' or ' ' || char.GetUnicodeCategory(c) == UnicodeCategory.Format;

    private static IEnumerable<CultureInfo> PredefinedCultures() =>
        CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => c.Name.Length > 0);

    [Fact]
    public void EveryPredefinedCulture_LocaleDateAndTime_CarryNoHostVaryingText()
    {
        var offenders = new List<string>();
        int withCultureData = 0;
        foreach (var culture in PredefinedCultures())
        {
            var facts = LocaleFacts.For(culture.Name);
            if (facts.HasCultureData) withCultureData++;
            var time = TimeFacts.Of(facts);
            // A morning and an evening time (both designators), a fraction (the decimal separator), the 24th hour.
            var results = new[]
            {
                time.FormatDate(new DateTime(2026, 10, 8)),
                time.FormatTime(9, 30, 0, null),
                time.FormatTime(21, 45, 7, "5"),
                time.FormatTime(24, 0, 0, null),
            };
            foreach (string r in results)
                if (r.Any(IsHostVarying))
                    offenders.Add($"{culture.Name}: {string.Join(" ", r.Select(c => IsHostVarying(c) ? $"<U+{(int)c:X4}>" : c.ToString()))}");
        }
        Assert.True(LocaleFacts.InvariantMode || withCultureData > 800, $"only {withCultureData} cultures resolved culture data - the sweep would be vacuous");
        Assert.True(offenders.Count == 0, $"{offenders.Count} culture(s) render host-varying text through LC_TIME (DETERMINATION L12):\n" + string.Join("\n", offenders.Take(20)));
    }

    [Fact]
    public void EveryPredefinedCulture_PatternLettersAreTokensTheRendererHandles()
    {
        var unhandled = new SortedSet<string>();
        foreach (var culture in PredefinedCultures())
        {
            var dtf = culture.DateTimeFormat;
            foreach (var (letters, pattern, kind) in new[] { (TimeFacts.DateTokenLetters, dtf.ShortDatePattern, "d_fmt"), (TimeFacts.TimeTokenLetters, dtf.LongTimePattern, "t_fmt") })
                foreach (char c in UnquotedAsciiLetters(pattern))
                    if (!letters.Contains(c))
                        unhandled.Add($"{kind} '{c}' ({culture.Name}: {pattern})");
        }
        Assert.True(unhandled.Count == 0, "pattern letters TimeFacts.Render would emit as literals:\n" + string.Join("\n", unhandled.Take(20)));
    }

    /// <summary>The ASCII letters of <paramref name="pattern"/> outside quotes and escapes — the format specifiers.</summary>
    private static IEnumerable<char> UnquotedAsciiLetters(string pattern)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c is '\'' or '"') { int close = pattern.IndexOf(c, i + 1); i = close < 0 ? pattern.Length : close; }
            else if (c == '\\') i++;
            else if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z') yield return c;
        }
    }

    /// <summary>⚖ DETERMINATION L13: the rendered date is the Gregorian date argument-1 names, whatever calendar the
    /// culture defaults to.</summary>
    [Theory]
    [InlineData("fa-IR", "2026/10/8")]     // Persian calendar by default: 1405/7/16
    [InlineData("th-TH", "8/10/2026")]     // Thai Buddhist by default: 8/10/2569
    public void TheDateRendered_IsTheGregorianDate(string tag, string expected)
    {
        if (LocaleFacts.InvariantMode || !LocaleFacts.For(tag).HasCultureData) return;
        Assert.Equal(expected, CobolLocale.Date("20261008", tag));
    }

    [Fact]
    public void ArabicAndSpanishTimes_AreTheDocumentedBytes()
    {
        if (LocaleFacts.InvariantMode || !LocaleFacts.For("ar-EG").HasCultureData) return;
        Assert.Equal("8/10/2026", CobolLocale.Date("20261008", "ar-EG"));                     // not 8 U+200F / 10 U+200F / 2026
        // The es-US designators are the HOST ICU's data, not a fixed byte string: the Windows ICU has "a.<U+00A0>m."
        // and the Linux ICU CI runs "a.m.". So the expected designator is the host's own, with L12's rule applied
        // independently here (Cf removed, U+00A0/U+202F/U+2009 to the plain space), and it must be free of them.
        var es = CultureInfo.GetCultureInfo("es-US").DateTimeFormat;
        Assert.Equal("9:30:00 " + Plain(es.AMDesignator), CobolLocale.Time("093000", "es-US")); // never a. U+00A0 m.
        Assert.Equal("9:45:00 " + Plain(es.PMDesignator), CobolLocale.Time("214500", "es-US"));
        Assert.DoesNotContain(CobolLocale.Time("093000", "es-US"), IsHostVarying);
    }

    /// <summary>DETERMINATION L12's rule, written independently of <see cref="LocaleFacts.NormalizeLocaleText"/>.</summary>
    private static string Plain(string s) =>
        string.Concat(s.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.Format)
                       .Select(c => IsHostVarying(c) ? ' ' : c));

    [Fact]
    public void TheNameTokens_RenderTheNormalizedGregorianNames()
    {
        if (LocaleFacts.InvariantMode || !LocaleFacts.For("fr-FR").HasCultureData) return;
        var date = new DateTime(2026, 10, 8);
        Assert.Equal("Thursday 8 October 2026", TimeFacts.Of(LocaleFacts.For("en-US")).FormatDate("dddd d MMMM yyyy", date));
        Assert.Equal("jeudi 8 octobre 2026", TimeFacts.Of(LocaleFacts.For("fr-FR")).FormatDate("dddd d MMMM yyyy", date));
        Assert.Equal("Thu Oct 08/10/26 26", TimeFacts.Of(LocaleFacts.For("en-US")).FormatDate("ddd MMM dd/MM/yy y", date));
        // The "/" of a pattern is the locale's date separator, normalized (ar-* carry U+200F in it).
        Assert.Equal("8/10/2026", TimeFacts.Of(LocaleFacts.For("ar-EG")).FormatDate("d/M/yyyy", date));
    }

    private static readonly Regex RawRead = new(@"\.(DateTimeFormat|NumberFormat)\b", RegexOptions.Compiled);

    /// <summary>The snapshot classes own the raw read: <see cref="LocaleFacts"/> publishes it, <see cref="TimeFacts"/>
    /// and <see cref="MonetaryFacts"/> normalize it, and <c>MonetaryPlacement</c> probes the INVARIANT culture's
    /// number format (no locale text).</summary>
    private static readonly string[] SnapshotFiles = ["LocaleFacts.cs", "TimeFacts.cs", "MonetaryFacts.cs", "MonetaryPlacement.cs"];

    [Fact]
    public void NoSourceOutsideTheSnapshotClasses_ReadsACulturesRawFormatData()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Root, file);
            if (rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || rel.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            if (SnapshotFiles.Contains(Path.GetFileName(file)) && rel.Contains("Globalization")) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("*", StringComparison.Ordinal) || t.StartsWith("/*", StringComparison.Ordinal)) continue;
                if (RawRead.IsMatch(t)) offenders.Add($"{rel}:{i + 1}  {t}");
            }
        }
        Assert.True(offenders.Count == 0,
            "a culture's raw DateTimeFormat / NumberFormat is read outside the snapshot classes (read it through TimeFacts / MonetaryFacts, which normalize it - DETERMINATION L12):\n" + string.Join("\n", offenders));
    }
}
