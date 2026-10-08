// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The per-edition conformance corpus manifests (roadmap Phase-1 shells; VERSION_TEST_MATRIX_DESIGN §5) — the DATA
/// half of the corpus runners, held in ONE non-generic type so the three partitions of
/// <see cref="CorpusRunnerTestsBase{TSlot}"/> share a single read of each <c>manifest.json</c>.
/// </summary>
internal static class ConformanceCorpus
{
    internal sealed record Manifest(IReadOnlyList<string> Enabled, IReadOnlyList<string> Pending);

    internal static string Root { get; } = TestRepo.Tests("conformance");

    // 85 carries the X3.23-1985-only goldens (the USE FOR DEBUGGING / DEBUG-ITEM facility, VCR 7.17 — a REMOVAL
    // gate whose ACCEPT edition is 85); 2002/2014/2023 carry the post-85 introductions.
    internal static string[] EditionDirs { get; } = ["85", "2002", "2014", "2023"];

    /// <summary>
    /// ⛔ A name listed TWICE (in one list, or in both) THROWS. A duplicate is the shape a union-resolved merge
    /// conflict leaves in these flat lists, and it is invisible to every local gate: the theory row is listed
    /// once per occurrence by <c>--list-tests</c> but executed once, so only CI's shard-population guard sees
    /// the difference (train 50: <c>2023/pb854_eop_perform_when</c> listed three times, "8073 of 8074").
    /// Refusing at load makes the local run fail first, and names the entry.
    /// </summary>
    internal static Manifest Load(string dir)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json")));
        static IReadOnlyList<string> Arr(JsonElement root, string name) =>
            [.. root.GetProperty(name).EnumerateArray().Select(e => e.GetString()!)];
        var manifest = new Manifest(Arr(doc.RootElement, "enabled"), Arr(doc.RootElement, "pending"));
        string[] dups = [.. manifest.Enabled.Concat(manifest.Pending)
            .GroupBy(n => n, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key)];
        if (dups.Length > 0)
        {
            throw new InvalidOperationException(
                $"{Path.Combine(dir, "manifest.json")} lists {string.Join(", ", dups)} more than once");
        }

        return manifest;
    }

    internal static IEnumerable<object[]> EnabledPositive()
    {
        foreach (string ed in EditionDirs)
        {
            foreach (string name in Load(Path.Combine(Root, ed)).Enabled)
            {
                yield return [ed, name];
            }
        }

        // xunit needs ≥1 row per theory; a sentinel keeps the theory alive while all entries are pending.
        yield return ["shell", "sentinel"];
    }

    internal static IEnumerable<object[]> EnabledNegative()
    {
        foreach (string name in Load(Path.Combine(Root, "negative")).Enabled)
        {
            yield return [name];
        }

        yield return ["sentinel"];
    }

    /// <summary>
    /// THE compile options of an enabled positive golden — written once, so the corpus runner and the architecture
    /// review's behavior-neutrality oracle (<see cref="ArchOracle"/>, kb/Work PB2116) compile it identically.
    /// <para>kb/Work PB803 — a golden may declare its own compile options in its leading comment block
    /// (<c>*&gt; options: sign-encoding=ascii</c>); one with no header compiles exactly as it always did. A golden's
    /// library text sits beside it, and the edition directory is named as a COPY search path (<c>--copy DIR</c>)
    /// exactly as a user names theirs: the default COBOL library (DOC-A.1-40, kb/Work PB1355) is the working
    /// directory plus the --copy directories, never the source file's own. kb/Work PB1362: the corpus was authored
    /// against reference-format DETECTION, so it compiles under the <c>--source-format auto</c> extension; a golden
    /// pinning the standard default says <c>source-format=fixed</c>.</para>
    /// </summary>
    internal static CobolNet.CompilerDriver.Options PositiveOptions(string edition, string name, string dll)
    {
        string dir = Path.Combine(Root, edition);
        string src = Path.Combine(dir, name + ".cob");
        return ApplySourceOptions(File.ReadAllText(src),
            new CobolNet.CompilerDriver.Options(src, dll, DialectLevel: int.Parse(edition), CopyPaths: [dir],
                SourceFormat: InitialReferenceFormat.Auto));
    }

    /// <summary>An enabled negative fixture's source text and the editions it must be REJECTED at. Convention:
    /// <c>&lt;name&gt;.cob</c>'s first line is a comment naming them — <c>*&gt; reject-at: 2002 2014 2023</c> — and a
    /// fixture without that header THROWS (a negative that names no edition would assert nothing).</summary>
    internal static (string Source, IReadOnlyList<int> Editions) NegativeCase(string name)
    {
        string src = File.ReadAllText(Path.Combine(Root, "negative", name + ".cob"));
        string first = src.Split('\n')[0];
        return first.Contains("reject-at:")
            ? (src, [.. first.Split("reject-at:")[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)])
            : throw new InvalidOperationException($"{name}.cob missing the '*> reject-at:' header");
    }

    /// <summary>
    /// ⛔ PER-GOLDEN COMPILE OPTIONS, declared in the PROGRAM'S OWN leading comment block (kb/Work PB803):
    /// <code>      *&gt; options: sign-encoding=ascii</code>
    /// Applied to <paramref name="baseline"/> and returned; a program with no such header compiles exactly as
    /// before.
    /// <para><b>Why the source and not <c>manifest.json</c>.</b> The manifest is a DISCOVERY register — two flat
    /// name lists whose only job is that nothing on disk goes unlisted — and the negative corpus already declares
    /// its per-case compile fact in the source (<c>*&gt; reject-at: 2002 2014 2023</c>, read by
    /// <see cref="CorpusRunnerTestsBase{TSlot}.EnabledNegativeCase_RejectsWithItsDiagnostic"/>). Putting the
    /// positive corpus's per-case fact in the same place keeps ONE convention for "what this program needs in
    /// order to compile", and keeps a golden and the way it must be built in one file that moves together.</para>
    /// <para>The line must <b>begin</b> with <c>*&gt; options:</c>, not merely contain it: a golden's comment block
    /// routinely QUOTES its own header while explaining itself, and a contains-test reads those quotations as
    /// declarations.</para>
    /// <para><b>An unknown key or an unrecognized value THROWS.</b> Silently ignoring one would compile the
    /// golden under the DEFAULT convention and report the result only as an output mismatch — a failure that
    /// names the wrong thing. The value vocabulary is not restated here: it is
    /// <c>ZonedSign.TryParseOption</c>, the same reader the CLI's <c>--sign-encoding</c> uses.</para>
    /// </summary>
    internal static CobolNet.CompilerDriver.Options ApplySourceOptions(
        string sourceText, CobolNet.CompilerDriver.Options baseline)
    {
        const string Header = "*> options:";
        var options = baseline;
        foreach (string raw in sourceText.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if (!line.StartsWith("*>", StringComparison.Ordinal)) break;   // past the leading comment block
            // ⛔ THE LINE MUST *START* WITH THE HEADER, never merely contain it. A golden's own comment block
            // routinely QUOTES the header while explaining itself ("compiles under `*> options: sign-encoding=
            // ascii`"), and a contains-test read those quotations as declarations and threw on the backtick —
            // three reds on the first gate run, none of them about the compiler.
            if (!line.StartsWith(Header, StringComparison.Ordinal)) continue;
            foreach (string token in line[Header.Length..]
                         .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = token.Split('=', 2);
                if (kv.Length != 2)
                    throw new InvalidOperationException($"'*> options:' entry '{token}' is not key=value");
                options = kv[0] switch
                {
                    "sign-encoding" => CobolNet.Runtime.ZonedSign.TryParseOption(kv[1], out var enc)
                        ? options with { SignEncoding = enc }
                        : throw new InvalidOperationException(
                            $"'*> options: sign-encoding={kv[1]}' is not one of "
                            + string.Join(", ", CobolNet.Runtime.ZonedSign.OptionSpellings)),
                    // kb/Work PB1362: the corpus runs under the auto-detection extension it was authored against; a
                    // golden that pins the STANDARD default (fixed form, §7.3.24.3 2)) or free form states it here.
                    "source-format" => InitialReferenceFormatOption.TryParse(kv[1], out var fmt)
                        ? options with { SourceFormat = fmt }
                        : throw new InvalidOperationException(
                            $"'*> options: source-format={kv[1]}' is not one of "
                            + string.Join(", ", InitialReferenceFormatOption.OptionSpellings)),
                    // kb/Work PB1525: ISO §4.2.10's warning mechanism (--flag-extensions). A golden that pins "the program
                    // still compiles and runs identically with extension flagging ON" states it here.
                    "flag-extensions" => kv[1] switch
                    {
                        "on" => options with { FlagExtensions = true },
                        "off" => options with { FlagExtensions = false },
                        _ => throw new InvalidOperationException(
                            $"'*> options: flag-extensions={kv[1]}' is not one of on, off"),
                    },
                    _ => throw new InvalidOperationException($"'*> options:' key '{kv[0]}' is not recognized"),
                };
            }
        }
        return options;
    }
}

/// <summary>
/// The per-edition corpus DISCOVERY RUNNERS: each edition directory under <c>tests/conformance/</c> carries a
/// <c>manifest.json</c> — ENABLED programs compile at that <c>--std</c> strict (and, given a sibling <c>.out</c>,
/// run and byte-compare); PENDING programs are catalogued but not asserted (the mass-red guard: most of the 2002
/// corpus exercises features the greenfield has not implemented yet — the wave that lands a feature flips its
/// programs to enabled). The NEGATIVE corpus (<c>tests/conformance/negative/</c>) inverts the contract: enabled
/// entries MUST fail with their <c>.err</c>-file diagnostic at each edition their manifest entry names.
/// </summary>
/// <remarks>
/// ⛔ THIS CLASS IS PARTITIONED — see <see cref="TestPartitioning"/> (plan §11 A13). It was the Conformance leg's
/// THIRD pole on the battery #41 trx (1,005 rows, 83.9 s serial), and once the version matrix and the NIST
/// differential are split it would BECOME the pole; xUnit 2.9.2 makes each test CLASS one collection. The
/// partitions keep <c>CorpusRunnerTests</c> in their names so every existing
/// <c>FullyQualifiedName~CorpusRunnerTests</c> filter (CI's shard matrix, plan §0) still selects them unchanged.
/// </remarks>
/// <typeparam name="TSlot">This partition's slot.</typeparam>
public abstract class CorpusRunnerTestsBase<TSlot>
    where TSlot : ITestPartitionSlot
{
    /// <summary>Partition count, chosen from the MEASURED serial cost (kb/Work PB2527, the w1033 gate diagnosis):
    /// at 3 partitions of about 1,546 cases each, each partition ran 246-282 s on one thread and the Conformance leg
    /// spent 95-220 s of its wall at 1-4 threads after its 32-wide phase. 24 partitions (about 190 cases, about 35 s
    /// each) end that tail; replayed onto 32 workers, the measured durations model the leg 247 s to 162 s, longest
    /// first. <c>CorpusPartitioningDriftTests</c> pins the count.</summary>
    public const int Partitions = 24;

    [PartitionedRowSource(nameof(EnabledPositive))]
    public static IEnumerable<object[]> AllEnabledPositive() => ConformanceCorpus.EnabledPositive();

    /// <summary>This partition's share of the enabled positive corpus.</summary>
    public static IEnumerable<object[]> EnabledPositive() =>
        TestPartitioning.SliceRows<TSlot>(AllEnabledPositive(), Partitions);

    [Theory]
    [MemberData(nameof(EnabledPositive))]
    public void EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(string edition, string name)
    {
        if (edition == "shell") return;   // the empty-manifest sentinel
        string dir = Path.Combine(ConformanceCorpus.Root, edition);
        string src = Path.Combine(dir, name + ".cob");
        // The RUN CONTRACT (landed with the first enabling wave — the W3 corpus audit, DEVLOG 597): an
        // enabled program with a sibling .out must compile STRICT at its edition, run, and byte-match the
        // expected output (line endings normalized — the .out files are LF; a Windows run emits CRLF).
        string outFile = Path.Combine(dir, name + ".out");
        string tmp = Path.Combine(Path.GetTempPath(), "CobolNet_Corpus_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tmp);
        try
        {
            string dll = Path.Combine(tmp, name + ".dll");
            var r = CompiledProgramCache.Compile(ConformanceCorpus.PositiveOptions(edition, name, dll));
            Assert.True(r.Success, $"[{edition}/{name}] must compile strict: {string.Join("\n", r.Errors)}");
            if (!File.Exists(outFile)) return;   // compile-only entry (no expected output recorded)
            var (ran, stdout, detail) = CutRunner.Run(dll, tmp);
            Assert.True(ran, $"[{edition}/{name}] must run: {detail}");
            // The same comparison basis as the NIST differential harness (CutRunner.Normalize — LF line
            // endings, per-line trailing-space trim, no trailing newline).
            Assert.Equal(CutRunner.Normalize(File.ReadAllText(outFile)), CutRunner.Normalize(stdout));
        }
        finally { try { Directory.Delete(tmp, recursive: true); } catch { /* best-effort */ } }
    }

    [PartitionedRowSource(nameof(EnabledNegative))]
    public static IEnumerable<object[]> AllEnabledNegative() => ConformanceCorpus.EnabledNegative();

    /// <summary>This partition's share of the enabled negative corpus.</summary>
    public static IEnumerable<object[]> EnabledNegative() =>
        TestPartitioning.SliceRows<TSlot>(AllEnabledNegative(), Partitions);

    [Theory]
    [MemberData(nameof(EnabledNegative))]
    public void EnabledNegativeCase_RejectsWithItsDiagnostic(string name)
    {
        if (name == "sentinel") return;
        string dir = Path.Combine(ConformanceCorpus.Root, "negative");
        string expected = File.ReadAllText(Path.Combine(dir, name + ".err")).Trim();
        var (src, editions) = ConformanceCorpus.NegativeCase(name);
        foreach (int ed in editions)
        {
            var (ok, errors, _) = EditionHarness.CompileFull(src, ed);
            Assert.False(ok, $"[negative/{name}] must be REJECTED at --std {ed}");
            EditionHarness.AssertHasDiagnostic(errors, expected);
        }
    }
}

// ⛔ THE 24 PARTITIONS — each its own xUnit collection. TestPartitionAudit proves they cover both corpora
// exactly once; CorpusPartitioningDriftTests pins the count (kb/Work PB2527).

/// <summary>Conformance-corpus partition 0 of <see cref="CorpusRunnerTestsBase{TSlot}.Partitions"/>.</summary>
public sealed class CorpusRunnerTests_P0 : CorpusRunnerTestsBase<Slot0>;

/// <summary>Conformance-corpus partition 1.</summary>
public sealed class CorpusRunnerTests_P1 : CorpusRunnerTestsBase<Slot1>;

/// <summary>Conformance-corpus partition 2.</summary>
public sealed class CorpusRunnerTests_P2 : CorpusRunnerTestsBase<Slot2>;

/// <summary>Conformance-corpus partition 3.</summary>
public sealed class CorpusRunnerTests_P3 : CorpusRunnerTestsBase<Slot3>;

/// <summary>Conformance-corpus partition 4.</summary>
public sealed class CorpusRunnerTests_P4 : CorpusRunnerTestsBase<Slot4>;

/// <summary>Conformance-corpus partition 5.</summary>
public sealed class CorpusRunnerTests_P5 : CorpusRunnerTestsBase<Slot5>;

/// <summary>Conformance-corpus partition 6.</summary>
public sealed class CorpusRunnerTests_P6 : CorpusRunnerTestsBase<Slot6>;

/// <summary>Conformance-corpus partition 7.</summary>
public sealed class CorpusRunnerTests_P7 : CorpusRunnerTestsBase<Slot7>;

/// <summary>Conformance-corpus partition 8.</summary>
public sealed class CorpusRunnerTests_P8 : CorpusRunnerTestsBase<Slot8>;

/// <summary>Conformance-corpus partition 9.</summary>
public sealed class CorpusRunnerTests_P9 : CorpusRunnerTestsBase<Slot9>;

/// <summary>Conformance-corpus partition 10.</summary>
public sealed class CorpusRunnerTests_P10 : CorpusRunnerTestsBase<Slot10>;

/// <summary>Conformance-corpus partition 11.</summary>
public sealed class CorpusRunnerTests_P11 : CorpusRunnerTestsBase<Slot11>;

/// <summary>Conformance-corpus partition 12.</summary>
public sealed class CorpusRunnerTests_P12 : CorpusRunnerTestsBase<Slot12>;

/// <summary>Conformance-corpus partition 13.</summary>
public sealed class CorpusRunnerTests_P13 : CorpusRunnerTestsBase<Slot13>;

/// <summary>Conformance-corpus partition 14.</summary>
public sealed class CorpusRunnerTests_P14 : CorpusRunnerTestsBase<Slot14>;

/// <summary>Conformance-corpus partition 15.</summary>
public sealed class CorpusRunnerTests_P15 : CorpusRunnerTestsBase<Slot15>;

/// <summary>Conformance-corpus partition 16.</summary>
public sealed class CorpusRunnerTests_P16 : CorpusRunnerTestsBase<Slot16>;

/// <summary>Conformance-corpus partition 17.</summary>
public sealed class CorpusRunnerTests_P17 : CorpusRunnerTestsBase<Slot17>;

/// <summary>Conformance-corpus partition 18.</summary>
public sealed class CorpusRunnerTests_P18 : CorpusRunnerTestsBase<Slot18>;

/// <summary>Conformance-corpus partition 19.</summary>
public sealed class CorpusRunnerTests_P19 : CorpusRunnerTestsBase<Slot19>;

/// <summary>Conformance-corpus partition 20.</summary>
public sealed class CorpusRunnerTests_P20 : CorpusRunnerTestsBase<Slot20>;

/// <summary>Conformance-corpus partition 21.</summary>
public sealed class CorpusRunnerTests_P21 : CorpusRunnerTestsBase<Slot21>;

/// <summary>Conformance-corpus partition 22.</summary>
public sealed class CorpusRunnerTests_P22 : CorpusRunnerTestsBase<Slot22>;

/// <summary>Conformance-corpus partition 23.</summary>
public sealed class CorpusRunnerTests_P23 : CorpusRunnerTestsBase<Slot23>;

/// <summary>
/// The corpus INTEGRITY facts — whole-directory assertions that make silent non-discovery impossible: every
/// on-disk program must be listed. They are about a manifest as a WHOLE, so they run ONCE and are deliberately not
/// on the partitioned base (an inherited theory would run once per partition and assert the same thing each time).
/// </summary>
public sealed class CorpusRunnerTests
{
    /// <summary>Every on-disk .cob is manifest-listed (enabled ⊕ pending) — nothing silently undiscovered.</summary>
    [Theory]
    [InlineData("85")]
    [InlineData("2002")]
    [InlineData("2014")]
    [InlineData("2023")]
    [InlineData("negative")]
    public void Manifest_CoversEveryProgram_NoOverlap(string edition)
    {
        string dir = Path.Combine(ConformanceCorpus.Root, edition);
        var m = ConformanceCorpus.Load(dir);
        var onDisk = Directory.EnumerateFiles(dir, "*.cob").Select(Path.GetFileNameWithoutExtension).ToHashSet();
        var listed = m.Enabled.Concat(m.Pending).ToHashSet();
        var unlisted = onDisk.Except(listed).Order().Take(5).ToList();
        var phantom = listed.Except(onDisk).Order().Take(5).ToList();
        Assert.True(unlisted.Count == 0, $"{edition}: unlisted programs (add to the manifest): {string.Join(", ", unlisted)}");
        Assert.True(phantom.Count == 0, $"{edition}: manifest lists missing programs: {string.Join(", ", phantom)}");
        Assert.Empty(m.Enabled.Intersect(m.Pending));
    }

    /// <summary>
    /// ⛔ THE PER-GOLDEN <c>*&gt; options:</c> HEADER — that it is read, that it is read only from the LEADING
    /// comment block, and that a typo FAILS rather than falling back to the default (kb/Work PB803).
    /// <para>The last of those is the one that matters. A golden whose option was silently dropped still compiles;
    /// it just compiles as a different program, and the only symptom would be an output mismatch that names the
    /// wrong cause. The scoping half matters for the same reason in reverse: a program that MOVEs the text
    /// "options: ..." must not be able to reconfigure its own compile.</para>
    /// </summary>
    [Fact]
    public void SourceOptionsHeader_IsScopedToTheCommentBlock_AndRefusesWhatItCannotHonour()
    {
        var baseline = new CobolNet.CompilerDriver.Options("x.cob", SourceFormat: InitialReferenceFormat.Auto);
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ibm,
            ConformanceCorpus.ApplySourceOptions("       IDENTIFICATION DIVISION.\n", baseline).SignEncoding);
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ascii,
            ConformanceCorpus.ApplySourceOptions(
                "      *> options: sign-encoding=ascii\n      *> more prose\n       IDENTIFICATION DIVISION.\n",
                baseline).SignEncoding);
        // Past the leading comment block the header is just text — a program cannot reconfigure its own compile.
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ibm,
            ConformanceCorpus.ApplySourceOptions(
                "       IDENTIFICATION DIVISION.\n      *> options: sign-encoding=ascii\n", baseline).SignEncoding);
        // ⛔ AND A COMMENT THAT MERELY QUOTES THE HEADER IS PROSE, not a declaration — the shape every one of
        // these goldens' own explanatory blocks has, and the one that turned a contains-test red on its first run.
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ibm,
            ConformanceCorpus.ApplySourceOptions(
                "      *> compiles under `*> options: sign-encoding=ascii`.\n", baseline).SignEncoding);
        Assert.Throws<InvalidOperationException>(() =>
            ConformanceCorpus.ApplySourceOptions("      *> options: sign-encoding=ebcdic\n", baseline));
        Assert.Throws<InvalidOperationException>(() =>
            ConformanceCorpus.ApplySourceOptions("      *> options: sign-encodng=ascii\n", baseline));
        Assert.Throws<InvalidOperationException>(() =>
            ConformanceCorpus.ApplySourceOptions("      *> options: sign-encoding\n", baseline));
    }

    /// <summary>The mechanism is LIVE in the corpus, not merely available: the one golden that needs the
    /// non-default convention really carries the header, so the option is exercised by a program that runs and
    /// byte-compares rather than only by a unit test of the parser.</summary>
    [Fact]
    public void TheAlternativeSignConventionGolden_CarriesItsOptionsHeader()
    {
        string src = File.ReadAllText(
            Path.Combine(ConformanceCorpus.Root, "2023", "pb803_sign_encoding_ascii.cob"));
        Assert.Equal(CobolNet.Runtime.SignEncoding.Ascii,
            ConformanceCorpus.ApplySourceOptions(src, new CobolNet.CompilerDriver.Options("x.cob", SourceFormat: InitialReferenceFormat.Auto)).SignEncoding);
    }
}
