// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Collation.Cldr;
using CobolNet.Runtime.Collation.Locale;
using Xunit;

namespace CobolNet.Tests.Unit.Collation;

/// <summary>
/// ⛔ THE FAILURE CONTRACT OF LOCALE-COLLATION RESOLUTION (kb/Work PB2773), one test per throw site. Every data problem
/// on the resolution path — a malformed site CLDR file (a negated UnicodeSet, broken XML), a malformed or stale site
/// <c>.tailor</c>, a collation the builder cannot number — is ONE type, <see cref="LocaleCollationException"/>, naming
/// its file; a problem with one RULE (a circular <c>[import]</c>, a <c>[before n]</c> with nothing before it) is listed
/// in <see cref="ResolvedLocaleCollation.Unsupported"/> and the rest of the collation is built. And every consumer
/// reaches its defined outcome instead of dying: <see cref="CollationEngine.ResolveLocale"/> records the failure over the
/// root order, <see cref="CollationEngine.TryGetOrderingTable"/> answers false, <see cref="CollationEngine.IsKnownLocale"/>
/// answers false, <see cref="CollationRuntime.Warmup"/> reports a Warning, and the compiler warns COBOLNET1662 naming the
/// file. Before, a malformed site <c>hu.xml</c> crashed the COMPILE of a legal ORDER TABLE program (FormatException) and a
/// stale <c>de.tailor</c> killed the first run unit at start-up under COBOL_COLLATION_WARMUP (InvalidOperationException).
/// <para>The site files use the private-use language codes qaa–qad (BCP 47), which no CLDR file and no other test names,
/// so a test class running beside this one never resolves a real locale through a broken file of this one.</para>
/// </summary>
[Collection("process-globals")]   // sets COBOL_CLDR_DIR / COBOL_COLLATION_DIR and clears the process-global caches
public sealed class LocaleCollationFailureTests
{
    private static readonly string PrivateUse = char.ConvertFromUtf32(0xE000);

    [Fact]
    public void AMalformedSiteCldrFile_IsTheResolutionFailure_NamingTheFile()
    {
        using var site = new SiteDirectory(CldrLocaleLoader.EnvDirectory);
        string file = site.Write("qaa.xml", Ldml("qaa", "[suppressContractions [^a]]"));

        var resolved = CollationEngine.ResolveLocale("qaa");
        Assert.NotNull(resolved.Failure);
        Assert.Equal(file, resolved.Failure!.DataSource);
        Assert.Contains("negated UnicodeSets", resolved.Failure.Reason);
        Assert.Same(CollationTable.Root, resolved.Table);                   // the nearest available order
        Assert.Contains(resolved.Failure.Message, resolved.Unsupported);
        Assert.False(CollationEngine.TryGetOrderingTable("qaa", out var table));
        Assert.Same(CollationTable.Root, table);
        Assert.False(CollationEngine.IsKnownLocale("qaa"));                 // not available in this environment
        Assert.Equal(file, Assert.Throws<LocaleCollationException>(() => LocaleManager.GetLocale("qaa")).DataSource);
        Assert.False(LocaleManager.TryGetLocale("qaa", out _));
    }

    [Fact]
    public void ASiteCldrFileThatIsNotXml_IsTheResolutionFailure_WithItsLine()
    {
        using var site = new SiteDirectory(CldrLocaleLoader.EnvDirectory);
        string file = site.Write("qaa.xml", "<ldml>\n<collations>\n<collation type=\"standard\">\n</ldml>\n");

        var failure = CollationEngine.ResolveLocale("qaa").Failure;
        Assert.NotNull(failure);
        Assert.Equal(file, failure!.DataSource);
        Assert.NotNull(failure.Line);
        Assert.IsType<System.Xml.XmlException>(failure.InnerException);
        Assert.False(CollationEngine.TryGetOrderingTable("qaa", out _));
    }

    [Fact]
    public void AStaleSiteTailoring_IsTheResolutionFailure_AndWarmupWarns()
    {
        using var site = new SiteDirectory(TailoringRules.EnvDirectory);
        string file = site.Write("qab.tailor", "@version 16.0.0\n");

        var failure = CollationEngine.ResolveLocale("qab").Failure;
        Assert.NotNull(failure);
        Assert.Equal(file, failure!.DataSource);
        Assert.Contains("UCA 16.0.0", failure.Reason);
        Assert.False(CollationEngine.TryGetOrderingTable("qab", out _));
        Assert.Throws<LocaleCollationException>(() => CollationTable.Root.WithTailoring(TailoringRules.Load(file)));

        var status = CollationRuntime.Warmup("qab");                         // used to throw InvalidOperationException
        Assert.False(status.DefaultLocaleResolved);
        Assert.Contains("UCA 16.0.0", status.Warning);
    }

    [Fact]
    public void AMalformedSiteTailoring_IsTheResolutionFailure_WithItsLine()
    {
        using var site = new SiteDirectory(TailoringRules.EnvDirectory);
        string file = site.Write("qad.tailor", "@locale qad\nU+00F1 [25718 0020 0002\n");

        Assert.False(CollationEngine.IsKnownLocale("qad"));
        var failure = CollationEngine.ResolveLocale("qad").Failure;
        Assert.NotNull(failure);
        Assert.Equal(file, failure!.DataSource);
        Assert.Equal(2, failure.Line);
        Assert.Contains("unterminated", failure.Reason);
        Assert.Equal(2, Assert.Throws<LocaleCollationException>(() => TailoringRules.Load(file)).Line);
    }

    /// <summary>A locale with a SOUND collation file of its own and a stale site <c>.tailor</c> over it: the file made
    /// the tag known before the tailoring was ever read, so the locale answered available and every comparison under it
    /// ran in the root order the failed resolution carries, silently (train 1055 review, kb/Work PB2773).</summary>
    [Fact]
    public void ASoundCollationFile_UnderAStaleSiteTailoring_IsNotAvailable()
    {
        using var cldr = new SiteDirectory(CldrLocaleLoader.EnvDirectory);
        cldr.Write("qac.xml", Ldml("qac", "&a<z"));
        using var site = new SiteDirectory(TailoringRules.EnvDirectory);
        site.Write("qac.tailor", "@version 16.0.0\n");

        Assert.NotNull(CollationEngine.ResolveLocale("qac").Failure);
        Assert.False(CollationEngine.IsKnownLocale("qac"));
        Assert.False(CollationEngine.TryGetOrderingTable("qac", out _));
        Assert.False(LocaleManager.TryGetLocale("qac", out _));
    }

    [Fact]
    public void ACircularImport_IsARuleProblem_ListedAndSkipped()
    {
        using var site = new SiteDirectory(CldrLocaleLoader.EnvDirectory);
        site.Write("qac.xml", Ldml("qac", "&a<z [import qac-u-co-standard]"));

        var resolved = CollationEngine.ResolveLocale("qac");
        Assert.Null(resolved.Failure);
        Assert.Contains(resolved.Unsupported, u => u.Contains("circular [import]") && u.Contains("nothing imported"));
        Assert.True(resolved.Collator.Compare("z", "b") < 0);                // the rest of the collation is built: &a<z
        Assert.True(CollationEngine.TryGetOrderingTable("qac", out _));
    }

    [Fact]
    public void ABeforeResetWithNothingBeforeIt_IsARuleProblem_ListedAndSkipped()
    {
        // [first secondary ignorable] has tertiary weight 0, the lowest of its line: nothing is before it at level 3.
        var result = Build("&[before 3][first secondary ignorable]<<<" + PrivateUse + " &a<z");
        Assert.Contains(result.Unsupported, u => u.Contains("[before 3] has no position before"));
        Assert.True(new Collator(result.Table, result.Options).Compare("z", "b") < 0);   // the other rule applied
    }

    [Fact]
    public void ACollationTheBuilderCannotNumber_IsTheResolutionFailure()
    {
        // More relations after an implicit-weight position (a Han character's second element) than the fixed room
        // between two implicit weights: the line cannot be numbered, so there is no table to build.
        int room = (1 << CollationTable.Root.PrimaryShift) - 1;
        var rules = new StringBuilder("&").Append(char.ConvertFromUtf32(0x4E00));
        for (int i = 0; i <= room; i++) rules.Append('<').Append(char.ConvertFromUtf32(0xF0000 + i));
        var ex = Assert.Throws<LocaleCollationException>(() => Build(rules.ToString()));
        Assert.Contains("implicit-weight position", ex.Reason);
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }

    [Fact]
    public void TheCompiler_WarnsCobolnet1662_NamingTheSiteFile_InsteadOfCrashing()
    {
        using var site = new SiteDirectory(CldrLocaleLoader.EnvDirectory);
        string file = site.Write("qaa.xml", Ldml("qaa", "[suppressContractions [^a]]"));
        string src = site.Write("ot.cob",
            "       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. PB2773OT.\n"
            + "       ENVIRONMENT DIVISION.\n"
            + "       CONFIGURATION SECTION.\n"
            + "       SPECIAL-NAMES.\n"
            + "           ORDER TABLE QAAORD IS \"qaa\".\n"
            + "       PROCEDURE DIVISION.\n"
            + "           DISPLAY \"OK\"\n"
            + "           STOP RUN.\n");
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, Path.Combine(site.Path, "ot.dll"),
            DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r.Success, string.Join("; ", r.Errors));
        Assert.Contains(r.Warnings, w => w.Contains("COBOLNET1662") && w.Contains(file) && w.Contains("negated UnicodeSets"));
    }

    private static string Ldml(string language, string rules) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ldml><identity><version number=\"1\"/><language type=\"" + language
        + "\"/></identity><collations><collation type=\"standard\"><cr><![CDATA[" + rules
        + "]]></cr></collation></collations></ldml>";

    private static CldrBuildResult Build(string rules)
    {
        var data = CldrParser.ParseXml(Ldml("qac", rules), "test rules");
        var selection = new CldrCollationSelection(CldrLocaleTag.Parse("qac"), data, data, data.Find("standard"), "standard", []);
        return CldrTailoringBuilder.Build(selection, "qac");
    }

    /// <summary>A temporary site directory named by the environment variable it is created for, with every
    /// process-global locale and collation cache cleared on entry, after each write and on exit, so no test sees
    /// another's site files.</summary>
    private sealed class SiteDirectory : IDisposable
    {
        private readonly string _variable;
        private readonly string? _saved;

        public SiteDirectory(string variable)
        {
            _variable = variable;
            _saved = Environment.GetEnvironmentVariable(variable);
            Path = Directory.CreateTempSubdirectory("cobolnet-pb2773-").FullName;
            Environment.SetEnvironmentVariable(variable, Path);
            LocaleManager.ClearCache();
        }

        public string Path { get; }

        public string Write(string name, string content)
        {
            string file = System.IO.Path.Combine(Path, name);
            File.WriteAllText(file, content);
            LocaleManager.ClearCache();
            return file;
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_variable, _saved);
            LocaleManager.ClearCache();
            try { Directory.Delete(Path, recursive: true); } catch (IOException) { /* best-effort temp cleanup */ }
        }
    }
}
