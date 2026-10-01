// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Locale;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.Globalization;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// The LOCALE intrinsics' runtime (Runtime/Intrinsics/CobolLocale.cs + Runtime/Globalization/LocaleFacts.cs;
/// DESIGN-locale-facility §4.7/§4.8/§8; kb/Work PB64 T4): LOCALE-COMPARE IS the locale-based comparison (§15.51.4
/// r2–r5); LOCALE-DATE/-TIME/-TIME-FROM-SECONDS render per the locale's <c>d_fmt</c>/<c>t_fmt</c> (L10 — the
/// culture's short date / long time patterns) with §15.53.3 r3's own hour/second ranges, the fraction of a scaled
/// seconds argument, EC-ARGUMENT-FUNCTION on invalid arguments; the three locale conditions — EC-LOCALE-MISSING (a
/// named unavailable locale), EC-LOCALE-INVALID (an available locale without culture data — a site-tailored made-up
/// tag), and the gating (nothing raised with checking off; the documented stand-in answers). The golden
/// tests/conformance/2014/pb64t4_locale_functions proves the same through compiled COBOL.
/// </summary>
[Collection("process-globals")]   // was "site-tailoring-directory"; widened (kb/Work PB126) — this class
                                  // clears the process-global locale/collation caches (LocaleManager.ClearCache),
                                  // which races any parallel identity-assert on the same caches.
public sealed class CobolLocaleTests
{
    [Fact]
    public void Compare_IsTheLocaleBasedRelation_WithTheSignMap()
    {
        RunUnit.Run(ru =>
        {
            Assert.Equal(">", CobolLocale.Compare("nz", "ñu", null));          // root: ñ = n + tilde, z > u
            Assert.Equal("<", CobolLocale.Compare("nz", "ñu", "es-ES"));       // Spanish: ñ a primary after n
            Assert.Equal("=", CobolLocale.Compare("ab", "ab   ", null));       // §15.51.4 r2 — trailing spaces
            Assert.Equal("=", CobolLocale.Compare("   ", " ", null));          // all spaces → one space
            Assert.Equal("<", CobolLocale.Compare("", "a", "fr-FR"));
            ru.Locale.SetFromLocale(LocaleCategorySet.Collate, "es-ES");        // the CURRENT locale (§14.6.6 r7)
            Assert.Equal("<", CobolLocale.Compare("nz", "ñu", null));
        });
    }

    [Fact]
    public void Date_RendersPerTheLocalesShortDatePattern_AndRejectsAnInvalidDate()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");                                 // the root (the test host's own culture is not the subject)
            Assert.Equal("08/19/2026", CobolLocale.Date("20260819", null));     // the root: invariant d_fmt
            Assert.Equal(new DateTime(2026, 8, 19).ToString(CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.ShortDatePattern, CultureInfo.GetCultureInfo("fr-FR")),
                CobolLocale.Date("20260819", "fr-FR"));
            // ⛔ EC-ARGUMENT-FUNCTION checking OFF: the §15.3 rule 14 substituted result is ONE
            // SPACE, not a zero-length value (kb/Work PB470). §15.52.4 r3 makes the returned length
            // depend on the LOCALE, which rejecting argument-1 leaves intact, so docs/CONFORMANCE.md row
            // DOC-A.1-90's zero-length class (a length derived from the REJECTED argument) does not reach
            // here and its general "spaces" clause does — one position, on §15.30.3 r1's own
            // answer for an alphanumeric function whose contents are absent. These asserts read the value
            // DIRECTLY because §14.6.8.5 space-fills a receiver from a zero-length sender, so a MOVE
            // cannot tell the two apart — which is how "" survived here for a year.
            Assert.Equal(" ", CobolLocale.Date("20261399", null));              // §15.52.3 r2 — month 13
            Assert.Equal(" ", CobolLocale.Date("2026081", null));               // r1 — 7 positions
            Assert.Equal(" ", CobolLocale.Date("16000101", null));              // year < 1601 — not a CURRENT-DATE value
            ExceptionState.ArgumentFunctionChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => CobolLocale.Date("20260230", null));
                Assert.Equal("EC-ARGUMENT-FUNCTION", ex.EcName);
            }
            finally { ExceptionState.ArgumentFunctionChecking = false; }
        });
    }

    [Fact]
    public void Time_RendersPerTheLocalesLongTimePattern_WithItsOwnRanges()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("13:05:09", CobolLocale.Time("130509", null));
            Assert.Equal("24:00:00", CobolLocale.Time("240000", null));          // §15.53.3 r3a — hour 24 is legal
            Assert.Equal("23:59:99", CobolLocale.Time("235999", null));          // r3b — seconds to 99
            // The §15.3 rule 14 substituted result — ONE SPACE, per row DOC-A.1-90 over
            // §15.53.4 r3 (see the LOCALE-DATE test above; kb/Work PB470). Both guards are read:
            // the r3 RANGE screen and the r1/r2 FORM screen were separate arms, each with its own
            // substitute, before they were moved onto the one rule table.
            Assert.Equal(" ", CobolLocale.Time("250000", null));                 // r3a — hour 25 is not
            Assert.Equal(" ", CobolLocale.Time("126000", null));                 // minute 60 is not
            Assert.Equal(" ", CobolLocale.Time("12345", null));                  // r1 — 5 positions
            var us = CultureInfo.GetCultureInfo("en-US");
            if (us.DateTimeFormat.LongTimePattern.Contains('t'))
                Assert.EndsWith(us.DateTimeFormat.PMDesignator, CobolLocale.Time("130509", "en-US"));
        });
    }

    [Fact]
    public void TimeFromSeconds_CarriesTheFraction_AndScreensTheStandardForm()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("13:05:09", CobolLocale.TimeFromSeconds(47109, 0, null));
            Assert.Equal("13:05:09.25", CobolLocale.TimeFromSeconds(4710925, 2, null));
            Assert.Equal("00:00:00", CobolLocale.TimeFromSeconds(0, 0, null));
            Assert.Equal("23:59:59", CobolLocale.TimeFromSeconds(86399, 0, null));
            // ONE SPACE again (row DOC-A.1-90 over §15.54.4 r3; kb/Work PB470) — and this guard
            // reaches it through a bool screening predicate that raised on its own, so the caller reads
            // ArgumentSubstitute.Spaces rather than spelling a literal.
            Assert.Equal(" ", CobolLocale.TimeFromSeconds(86400, 0, null));      // §7.3.17.4 GR5 — not standard form
            // GR4 (LEAP-SECOND ON): 86 400 IS the day's leap second, the time 23:59:60 (§15.54.4 r2 — "the hours,
            // minutes, and seconds of the time specified by argument-1"; §15.3.3.3 lets the seconds subfield be 60),
            // the same reading FORMATTED-TIME gives it. It was "24:00:00" — a time the day does not have (PB1379).
            Assert.Equal("23:59:60", CobolLocale.TimeFromSeconds(86400, 0, null, leapSecond: true));
            Assert.Equal("23:59:60.5", CobolLocale.TimeFromSeconds(864005, 1, null, leapSecond: true));
            Assert.Equal(" ", CobolLocale.TimeFromSeconds(-1, 0, null));         // GR5 — negative
            var fr = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("13:05:09" + fr.NumberFormat.NumberDecimalSeparator + "250", CobolLocale.TimeFromSeconds(47109250, 3, "fr-FR"));
        });
    }

    [Fact]
    public void FormatTime_HandlesTheTokenVocabulary()
    {
        var facts = LocaleFacts.Root;
        // Tokens rendered against a hand-built pattern through the invariant culture's designators/separators.
        Assert.Equal("13:05:09", CobolLocale.FormatTime(facts, 13, 5, 9, null));
        var us = LocaleFacts.For("en-US");
        string rendered = CobolLocale.FormatTime(us, 0, 7, 3, null);
        Assert.Contains("12", rendered);                                          // h/hh: hour 0 renders as 12 on a 12-hour pattern
        Assert.Contains(us.DateTimeFormat.AMDesignator, rendered);
        Assert.True(us.TimeFormat.Length > 0 && us.DateFormat.Length > 0);
    }

    [Fact]
    public void NamedUnavailableLocale_IsMissing_AndTheRootAnswers()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("08/19/2026", CobolLocale.Date("20260819", "xx-NOWHERE"));   // checking off: the root's answer
            Assert.Equal(">", CobolLocale.Compare("nz", "ñu", "xx-NOWHERE"));
            ExceptionState.LocaleMissingChecking = true;
            try
            {
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.Date("20260819", "xx-NOWHERE")).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.Time("130509", "xx-NOWHERE")).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.TimeFromSeconds(1, 0, "xx-NOWHERE")).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.Compare("a", "b", "xx-NOWHERE")).EcName);
                Assert.Equal("<", CobolLocale.Compare("nz", "ñu", "es-ES"));    // an available one: no raise
            }
            finally { ExceptionState.LocaleMissingChecking = false; }
        });
    }

    /// <summary>⚖ DETERMINATION L10 addendum (kb/Work PB112 — CI's Linux-only red): the U+202F / U+2009 spacing newer
    /// ICU releases put in date/time patterns is normalized to the plain space, so LOCALE-TIME's output is IDENTICAL
    /// on every host (Windows' bundled ICU renders en-US's day-period separator as ' ', Linux's ICU ≥ 72 as U+202F —
    /// the T4 golden failed only on the Linux runners, on a byte no assert message could show). The end-to-end pin
    /// asserts the PLAIN-SPACE byte on the host whose ICU says otherwise; the normalizer pin is host-independent.</summary>
    [Fact]
    public void TimePatternSpacing_IsHostStable()
    {
        Assert.Equal("h:mm:ss tt", LocaleFacts.NormalizeSpacing("h:mm:ss\u202Ftt"));
        Assert.Equal("h:mm:ss tt", LocaleFacts.NormalizeSpacing("h:mm:ss\u2009tt"));
        Assert.DoesNotContain('\u202F', LocaleFacts.For("en-US").TimeFormat);
        Assert.DoesNotContain('\u2009', LocaleFacts.For("en-US").TimeFormat);
        if (!LocaleFacts.InvariantMode && LocaleFacts.For("en-US").HasCultureData)
            RunUnit.Run(ru =>
            {
                ru.Locale.Set(LocaleCategory.All, "");
                // The T4 golden's TIME-US observable, byte-for-byte: a PLAIN U+0020 before the designator, whatever
                // the host ICU's CLDR vintage says (the golden tests/conformance/2014/pb64t4_locale_functions.out
                // pins the same bytes at the corpus level).
                Assert.Equal("1:05:09 PM", CobolLocale.Time("130509", "en-US"));
            });
    }

    /// <summary>UPPER-CASE / LOWER-CASE under a locale (T5): the LOCALE phrase (a tag) and the CHARACTER CLASSIFICATION
    /// (LocaleFacts) — DETERMINATION L9's simple map through the culture's TextInfo (Turkish dotted/dotless I); null
    /// facts is the invariant map; EC-LOCALE-MISSING for an unavailable named locale.</summary>
    [Fact]
    public void CaseMapping_FollowsTheLocalesLcCtype()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Equal("\u0130", CobolLocale.UpperCase("i", "tr-TR"));           // dotted capital I
            Assert.Equal("\u0131", CobolLocale.LowerCase("I", "tr-TR"));           // dotless small i
            Assert.Equal("I", CobolLocale.UpperCase("i", (LocaleFacts?)null));    // the implementor's map (§15.97.4 r4)
            Assert.Equal("I", CobolLocale.UpperCase("i", LocaleFacts.For("en-US")));
            Assert.Equal("\u0130", CobolLocale.UpperCase("i", LocaleFacts.For("tr")));
            Assert.Equal("", CobolLocale.UpperCase(null, "fr-FR"));
            Assert.Equal("ABC", CobolLocale.UpperCase("abc", ""));                 // the root
            // §15.97.4 r6 / §15.57.4 r6 — a letter with no correspondence is UNCHANGED (ß has no simple uppercase; the
            // simple map of L9 never expands it to SS), under a locale and under the implementor's map alike.
            Assert.Equal("STRA\u00DFE", CobolLocale.UpperCase("stra\u00DFe", "de-DE"));
            Assert.Equal("STRA\u00DFE", CobolLocale.UpperCase("stra\u00DFe", (LocaleFacts?)null));
            Assert.Equal(6, CobolLocale.UpperCase("stra\u00DFe", "de-DE").Length);   // r5 — the length is argument-1's
            ExceptionState.LocaleMissingChecking = true;
            try { Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.UpperCase("a", "xx-NOWHERE")).EcName); }
            finally { ExceptionState.LocaleMissingChecking = false; }
            Assert.Equal("A", CobolLocale.UpperCase("a", "xx-NOWHERE"));           // checking off: the root's answer stands
        });
    }

    /// <summary>The ONE §8.2.1 gate for the classification consumers (T5): a classification naming a DECLARED but
    /// UNAVAILABLE locale resolves (the compiler never checks availability — L1) and every operation requiring it —
    /// a class test, a case function without a phrase — raises EC-LOCALE-MISSING at use when checking is on, the coded
    /// character set's behavior standing when it is off; an available locale whose culture data .NET lacks raises
    /// EC-LOCALE-INVALID the same way (when the process HAS culture data at all).</summary>
    [Fact]
    public void Classification_UnavailableLocale_RaisesMissingAtUse()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            var missing = CharacterClassification.Resolve(LocalePhraseKind.Named, "xx-NOWHERE", LocalePhraseKind.None, null);
            Assert.NotNull(missing.Alphanumeric);
            Assert.False(missing.Alphanumeric!.IsAvailable);
            // checking off — the coded character set's set (GR3 b2: space included) and the implementor's map stand
            Assert.True(CobolClass.IsAlphabetic("ab cd", missing.Alphanumeric));
            Assert.Equal("I", CobolLocale.UpperCase("i", missing.Alphanumeric));
            ExceptionState.LocaleMissingChecking = true;
            try
            {
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolClass.IsAlphabetic("ab", missing.Alphanumeric)).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolClass.IsAlphabeticUpper("AB", missing.Alphanumeric)).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolClass.IsAlphabeticLower("ab", missing.Alphanumeric)).EcName);
                Assert.Equal("EC-LOCALE-MISSING", Assert.Throws<CobolFatalException>(() => CobolLocale.LowerCase("A", missing.Alphanumeric)).EcName);
                var tr = CharacterClassification.Resolve(LocalePhraseKind.Named, "tr-TR", LocalePhraseKind.None, null);
                Assert.True(CobolClass.IsAlphabetic("\u0131", tr.Alphanumeric));       // an available locale raises nothing
            }
            finally { ExceptionState.LocaleMissingChecking = false; }
            // the INVALID arm (available, no culture data) needs a site tailoring — AvailableLocaleWithoutCultureData_IsInvalid…
        });
    }

    /// <summary>The CHARACTER CLASSIFICATION resolution at activation (§12.3.6.4 GR5; §14.6.6 r2): the four phrase
    /// kinds, the None singleton, and the classification-aware class tests (§8.8.4.4.4 GR3 b1/c1/d1 — a Unicode letter
    /// per LC_CTYPE; no space; the locale's case round-trip for upper/lower).</summary>
    [Fact]
    public void Classification_ResolvesAtActivation_AndDrivesTheClassTests()
    {
        RunUnit.Run(ru =>
        {
            ru.Locale.Set(LocaleCategory.All, "");
            Assert.Same(CharacterClassification.None, CharacterClassification.Resolve(LocalePhraseKind.None, null, LocalePhraseKind.None, null));
            var named = CharacterClassification.Resolve(LocalePhraseKind.Named, "tr-TR", LocalePhraseKind.None, null);
            Assert.Equal("tr-TR", named.Alphanumeric!.Culture.Name);
            Assert.Null(named.National);
            ru.Locale.SetFromLocale(LocaleCategorySet.Ctype, "de-DE");
            var current = CharacterClassification.Resolve(LocalePhraseKind.Current, null, LocalePhraseKind.Current, null);
            Assert.Equal("de-DE", current.Alphanumeric!.Culture.Name);                 // the locale current at activation
            Assert.Equal("de-DE", current.For(national: true)!.Culture.Name);
            ru.Locale.SetFromLocale(LocaleCategorySet.Ctype, "fr-FR");
            Assert.Equal("de-DE", current.Alphanumeric!.Culture.Name);                 // a later SET does not move it (GR8)
            var user = CharacterClassification.Resolve(LocalePhraseKind.UserDefault, null, LocalePhraseKind.SystemDefault, null);
            Assert.Equal(ru.Locale.UserDefault.Ctype, user.Alphanumeric!.Collate);
            Assert.Equal(ru.Locale.SystemDefault.Ctype, user.National!.Collate);
            // the class tests
            var tr = LocaleFacts.For("tr-TR");
            Assert.True(CobolClass.IsAlphabetic("\u0131\u0130", tr));                  // dotless/dotted I are letters
            Assert.False(CobolClass.IsAlphabetic("\u0131", (LocaleFacts?)null));       // the Latin set without a locale
            Assert.False(CobolClass.IsAlphabetic("ab cd", tr));                         // GR3 b1: space is not alpha
            Assert.True(CobolClass.IsAlphabetic("ab cd", (LocaleFacts?)null));         // b2 names space
            Assert.True(CobolClass.IsAlphabeticUpper("\u0130I", tr));
            Assert.True(CobolClass.IsAlphabeticLower("\u0131i", tr));
            Assert.False(CobolClass.IsAlphabeticUpper("\u0131", tr));
            Assert.False(CobolClass.IsAlphabetic("", tr));                              // a zero-length operand is false (GR1)
            Assert.False(CobolClass.IsAlphabetic("a1", tr));
        });
    }

    /// <summary>EC-LOCALE-INVALID (§8.2.1 "invalid or incomplete"): a locale that IS available — a site tailoring
    /// makes "zz-QQ" known — but has no .NET culture data for LC_TIME; the invariant format stands when checking is
    /// off and the condition is raised when on. LC_COLLATE is unaffected (the tailoring collates).</summary>
    [Fact]
    public void AvailableLocaleWithoutCultureData_IsInvalid_ForTheTimeFunctions()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cobolnet-locales-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string saved = Environment.GetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable) ?? "";
        try
        {
            File.WriteAllText(Path.Combine(dir, "zz-QQ.tailor"), "@locale zz-QQ\n");
            Environment.SetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable, dir);
            LocaleManager.ClearCache();
            LocaleFacts.ClearCache();
            Assert.True(LocaleIdentification.IsAvailable("zz-QQ"));
            var facts = LocaleFacts.For("zz-QQ");
            Assert.False(facts.HasCultureData);
            Assert.Equal(CultureInfo.InvariantCulture, facts.Culture);
            RunUnit.Run(ru =>
            {
                ru.Locale.Set(LocaleCategory.All, "");
                Assert.Equal("08/19/2026", CobolLocale.Date("20260819", "zz-QQ"));   // checking off: the invariant stand-in
                Assert.Equal(">", CobolLocale.Compare("nz", "ñu", "zz-QQ"));        // LC_COLLATE is the tailoring (root order)
                ExceptionState.LocaleInvalidChecking = true;
                try
                {
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolLocale.Date("20260819", "zz-QQ")).EcName);
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolLocale.Time("130509", "zz-QQ")).EcName);
                    Assert.Equal("08/19/2026", CobolLocale.Date("20260819", null));     // the root has its data
                    Assert.Equal("19/08/2026", CobolLocale.Date("20260819", "fr-FR"));  // so has France
                    // the T5 consumers ride the SAME gate (LocaleFacts.Require): a CHARACTER CLASSIFICATION naming zz-QQ
                    // resolves, and its class tests / case functions raise EC-LOCALE-INVALID at use under checking
                    var cls = CharacterClassification.Resolve(LocalePhraseKind.Named, "zz-QQ", LocalePhraseKind.None, null);
                    Assert.True(cls.Alphanumeric!.IsAvailable);
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolClass.IsAlphabetic("ab", cls.Alphanumeric)).EcName);
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolLocale.UpperCase("ab", cls.Alphanumeric)).EcName);
                    Assert.Equal("EC-LOCALE-INVALID", Assert.Throws<CobolFatalException>(() => CobolLocale.LowerCase("AB", "zz-QQ")).EcName);   // the LOCALE phrase form too
                }
                finally { ExceptionState.LocaleInvalidChecking = false; }
                // checking off: no LC_CTYPE content — the coded character set's Latin set (space included) and the invariant map stand
                var cls2 = CharacterClassification.Resolve(LocalePhraseKind.Named, "zz-QQ", LocalePhraseKind.None, null);
                Assert.True(CobolClass.IsAlphabetic("ab cd", cls2.Alphanumeric));
                Assert.Equal("AB", CobolLocale.UpperCase("ab", cls2.Alphanumeric));
            });
            Assert.True(LocaleFacts.For("fr-FR").HasCultureData);
            Assert.True(LocaleFacts.For("es_ES.UTF-8").HasCultureData);             // L1-normalized before lookup
            Assert.Equal("es-ES", LocaleFacts.For("es_ES.UTF-8").Culture.Name);
            Assert.True(LocaleFacts.For("de-u-co-phonebook").HasCultureData);        // the -u- extension is stripped for culture data
        }
        finally
        {
            Environment.SetEnvironmentVariable(LocaleConfig.TailoringDirectoryVariable, saved.Length == 0 ? null : saved);
            LocaleManager.ClearCache();
            LocaleFacts.ClearCache();
            Directory.Delete(dir, recursive: true);
        }
    }
}
