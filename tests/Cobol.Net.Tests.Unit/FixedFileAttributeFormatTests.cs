// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// A physical file's §9.1.6 fixed file attributes AS ITS OWN FORMAT RECORDS THEM, and the §14.9.27.4 GR10
/// comparison the OPEN statement performs against them — kb/Work PB802 (owner decision 2026-09-07: <i>"Let's
/// match GNUCobol's implementation in spirit. No sidecar of any type."</i>), replacing kb/Work PB193's catalog.
/// The end-to-end behaviour per organization rides the goldens (<c>85/open_fixed_attribute_conflict</c>,
/// <c>2023/open_fixed_attribute_conflict_ix</c>); these lock the properties a golden CANNOT see:
/// <list type="bullet">
/// <item>the attributes are on DISK and are the DATA FILE — a keyed OPEN OUTPUT leaves exactly one artifact
/// behind, and its first bytes are the header, so a LATER RUN sees the whole of what was recorded;</item>
/// <item>a store this build cannot interpret is a '39' rather than a silent misread, and a ZERO-BYTE file is
/// the one tolerated exception;</item>
/// <item>the <c>COBOLNET_KEYCHECK=OFF</c> opt-out, which no COBOL program can set;</item>
/// <item>the per-organization dispatch cannot silently lose an arm.</item>
/// </list>
/// </summary>
public sealed class FixedFileAttributeFormatTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_FFA_" + Guid.NewGuid().ToString("N")[..8]);
    private readonly string? _keyCheckWas = Environment.GetEnvironmentVariable(FileRegistry.KeyCheckVariable);

    public FixedFileAttributeFormatTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(FileRegistry.KeyCheckVariable, _keyCheckWas);
        CobolFile.Init();   // the next test's registry must not inherit this one's key-check answer
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private string Host(string name) => Path.Combine(_dir, name);

    /// <summary>Create a physical file through a RELATIVE connector of 10-byte records — §14.9.27.4 GR18 makes
    /// the OPEN OUTPUT a creation, which is where §9.1.6 fixes a file's attributes.</summary>
    private static void MakeRelativeTenByteFile(string host)
    {
        CobolFile.Init();
        CobolFile.RegisterRelative("MK", host, 10, false, 0, 0);
        CobolFile.OpenOutput("MK", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("MK"));
        CobolFile.Close("MK");
    }

    /// <summary>Create a physical file through a record-SEQUENTIAL connector of fixed 20-byte records, holding
    /// one record — the shape a print, report or extract file has.</summary>
    private static void MakeSequentialTwentyByteFile(string host)
    {
        CobolFile.Init();
        CobolFile.Register("MK", host, 20, lineSequential: false, optional: false);
        CobolFile.OpenOutput("MK", host, assignDynamic: false, page: null);
        CobolFile.Write("MK", "ABCDEFGHIJKLMNOPQRST", -1, page: null);
        CobolFile.Close("MK");
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("MK"));
    }

    /// <summary>A fresh run unit whose only connector opens the file INPUT as a RELATIVE store of 40-byte
    /// records — a contradiction of both the organization and the record size of every subject below.</summary>
    private static string OpenContradictingRelativeConnector(string host)
    {
        CobolFile.Init();   // ⛔ a NEW registry: no connector and no status survives from above
        CobolFile.RegisterRelative("RD", host, 40, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        return CobolFile.Status("RD");
    }

    // ── The attributes ARE the file ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheStoreHeaderIsTheONLYArtifact_AndItIsInsideTheDataFile()
    {
        string host = Host("ffa-durable.dat");
        MakeRelativeTenByteFile(host);

        // ⛔ "No sidecar of any type" (owner decision 2026-09-07) is a statement about the DIRECTORY, so it is
        // measured there: creating a keyed file leaves exactly ONE artifact. NTFS alternate data streams were
        // rejected with the sidecar, and one is what a `*.dat:*` enumeration would add here.
        Assert.Equal([Path.GetFileName(host)], Directory.GetFiles(_dir).Select(Path.GetFileName).Order());

        // The whole of what a LATER RUN can know is these bytes: magic + version, 'R'elative, 'F'ixed,
        // min 10, max 10, no keys. Nothing in this process holds them.
        byte[] head = File.ReadAllBytes(host);
        Assert.Equal(
            [(byte)'C', (byte)'B', (byte)'N', (byte)'F', (byte)'S', (byte)'T', (byte)'R', 1,
             (byte)'R', (byte)'F', 10, 0, 0, 0, 10, 0, 0, 0, 0, 0],
            head[..20]);
        Assert.Equal(20, head.Length);   // an empty store is its header and nothing else

        // §14.9.27.4 GR10 — organization and record size both differ: the OPEN is unsuccessful, '39'.
        Assert.Equal(FileStatusCode.FixedAttributeConflict, OpenContradictingRelativeConnector(host));
    }

    [Fact]
    public void MatchingConnectorOpensNormally()
    {
        string host = Host("ffa-match.dat");
        MakeRelativeTenByteFile(host);
        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 10, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));   // the check rejects only real conflicts
        CobolFile.Close("RD");
    }

    [Theory]
    // A byte of the MAGIC is wrong — a file some other tool wrote, or a store from before the header existed.
    [InlineData(0, (byte)'X')]
    [InlineData(6, (byte)'X')]
    // A FORMAT VERSION this build does not know. It is a CONFLICT and not "attributes not recorded": an
    // unknown layout cannot have its frames located either, so reading on would be a silent misread.
    [InlineData(7, (byte)9)]
    // An ORGANIZATION byte outside the two a framed store can carry (§9.1.6 names three, and the sequential
    // one has no store) — and a RECORD TYPE outside fixed/variable.
    [InlineData(8, (byte)'S')]
    [InlineData(9, (byte)'X')]
    public void AStoreThisBuildCannotInterpret_IsAConflict(int at, byte to)
    {
        string host = Host($"ffa-foreign-{at}-{to}.dat");
        MakeRelativeTenByteFile(host);
        byte[] bytes = File.ReadAllBytes(host);
        bytes[at] = to;
        File.WriteAllBytes(host, bytes);

        // Even the connector that MATCHES the file's declared attributes is refused, because the refusal is
        // about the format and not about the attributes: nothing here can be read back to be compared.
        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 10, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void AZeroByteFileIsNotAConflict()
    {
        // The ONE tolerated shape: a file with no bytes holds no records, so it can misread nothing, and it
        // states no §9.1.6 attribute to contradict. A validated set must never MANUFACTURE a '39'.
        string host = Host("ffa-empty.dat");
        File.WriteAllBytes(host, []);
        Assert.Equal(FileStatusCode.Success, OpenContradictingRelativeConnector(host));
    }

    [Fact]
    public void DeleteFileDestroysTheOnlyArtifactThereIs()
    {
        // Under the catalog sidecar this needed a second delete, keyed on the whole success family, or a stale
        // catalog would judge a DIFFERENT file later created at the same path. With the attributes inside the
        // file, destroying the file destroys them and the question does not arise (kb/Work PB802).
        string host = Host("ffa-delete.dat");
        MakeRelativeTenByteFile(host);

        CobolFile.Init();
        CobolFile.RegisterRelative("DK", host, 10, false, 0, 0);
        Assert.Equal(FileStatusCode.Success, CobolFile.DeleteFile("DK"));
        Assert.Empty(Directory.GetFiles(_dir));

        // ... and a file later created at that path by an OPEN OUTPUT states its OWN attributes, which the
        // once-contradicting connector now matches.
        CobolFile.Init();
        CobolFile.RegisterRelative("WR", host, 40, false, 0, 0);
        CobolFile.OpenOutput("WR", host, assignDynamic: false, page: null);
        CobolFile.Close("WR");
        Assert.Equal(FileStatusCode.Success, OpenContradictingRelativeConnector(host));
    }

    // ── The validated set is what each organization's FORMAT encodes (Annex A.1 item 129) ────────────────────
    //    A set that is too broad rejects legal source just as surely as one that is too narrow reads rubbish,
    //    so every arm below is pinned in BOTH directions.

    [Theory]
    [InlineData(30, false)]   // a WIDER record description over the same record-sequential file
    [InlineData(9, false)]    // a NARROWER one — the report-file read-back shape
    [InlineData(30, true)]    // and a LINE SEQUENTIAL one: §9.1.6's record delimiter, also not recorded
    public void ASequentialFilesRecordSizeAndDelimiterAreNotValidated(int width, bool lineSequential)
    {
        // §9.1.7.2 puts a sequential file's record lengths in the DATA and in the READING program, not in the
        // file: "In record sequential files the length of each record is determined by any information the
        // implementor may add to the record on the physical storage medium (such as record length headers)" —
        // WiseOwl COBOL adds none to a fixed-length one — and "In line sequential files the length of each record
        // is determined by the number of characters between the preceding line delimiter and the following
        // line delimiter or the end of file if no line delimiter is present". The standard answers the
        // resulting disagreement with a SUCCESSFUL completion, §9.1.13.2 item 3's '04' and item 5's '06', so
        // GR10's '39' must NOT fire first — writing a print or report file and reading it back under a
        // different record description is exactly the idiom those statuses exist for.
        string host = Host($"ffa-seq-{width}-{lineSequential}.dat");
        MakeSequentialTwentyByteFile(host);

        CobolFile.Init();
        CobolFile.Register("RD", host, width, lineSequential, optional: false);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    [Fact]
    public void ASequentialFileOpenedThroughAKeyedDescription_IsAConflict()
    {
        // The direction the FORMAT can see: a plain byte stream carries no store header, so a RELATIVE (or
        // INDEXED) description cannot interpret it at all — GR10's '39'.
        string host = Host("ffa-seq-as-relative.dat");
        MakeSequentialTwentyByteFile(host);

        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 20, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void AKeyedStoreOpenedThroughASequentialDescription_IsNOTAConflict()
    {
        // ⛔ THE OTHER DIRECTION, AND WHAT THE OWNER'S DECISION GIVES UP (kb/Work PB802 — it is pinned so that
        // it is a decision and not a regression nobody noticed). kb/Work PB193's original reproduction was
        // exactly this shape and answered '39' from the catalog. A SEQUENTIAL file's format records nothing,
        // so nothing in it contradicts a sequential description and the OPEN succeeds — which is also what the
        // surveyed implementation does. RecordLayoutNotice still reports the byte-count disagreement on stderr.
        string host = Host("ffa-rel-as-seq.dat");
        MakeRelativeTenByteFile(host);

        CobolFile.Init();
        CobolFile.Register("RD", host, 40, lineSequential: false, optional: false);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    [Fact]
    public void ARelativeStoresRecordSizeIsValidated()
    {
        // The complement, and the reason the determination is per organization rather than uniform: a relative
        // store is an implementor-defined structure whose slot layout IS the record size, and the header states
        // it — so a description that disagrees cannot interpret the store: GR10's '39'.
        string host = Host("ffa-rel-size.dat");
        MakeRelativeTenByteFile(host);

        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 40, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void ARelativeStoresRecordTypeIsValidated()
    {
        // §9.1.6's "record type (fixed or variable)". The MAXIMUM agrees here and the type does not, so this
        // fails only if the type is genuinely compared rather than falling out of a size test.
        string host = Host("ffa-rel-type.dat");
        MakeRelativeTenByteFile(host);

        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 10, false, 0, 0, varyMin: 1, varyMax: 10);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    // ── The RECORD VARYING record-sequential arm: §9.1.7.2's record-length header must PARSE ─────────────────

    [Fact]
    public void AVaryingRecordSequentialFileWhoseFramingDoesNotParse_IsAConflict()
    {
        // The subject is a plain 20-byte text file: its first four bytes ("ABCD") read as a length prefix name
        // 0x44434241 bytes, ~1.1 GB, which the file does not hold. That is not a framed file, so a connector
        // declaring the VARIABLE record type contradicts the medium — '39' (§9.1.13.6 item 7).
        // ⚖ Behaviourally GnuCOBOL's "variable-length SEQUENTIAL data integrity" case.
        string host = Host("ffa-vary-foreign.dat");
        MakeSequentialTwentyByteFile(host);

        CobolFile.Init();
        CobolFile.Register("RD", host, 40, lineSequential: false, optional: false, varyMin: 5, varyMax: 40);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void AVaryingRecordSequentialFileWrittenBySuchAConnector_OpensAndRoundTrips()
    {
        // The other direction: the framing this processor writes parses, so the OPEN succeeds. Without this the
        // check could pass by refusing every varying file.
        string host = Host("ffa-vary-own.dat");
        CobolFile.Init();
        CobolFile.Register("WR", host, 40, lineSequential: false, optional: false, varyMin: 5, varyMax: 40);
        CobolFile.OpenOutput("WR", host, assignDynamic: false, page: null);
        CobolFile.Write("WR", "HELLO", 5, page: null);
        CobolFile.Close("WR");

        CobolFile.Init();
        CobolFile.Register("RD", host, 40, lineSequential: false, optional: false, varyMin: 5, varyMax: 40);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    [Fact]
    public void AVaryingPrefixOutsideTheRecordClausesBounds_IsNotAConflict()
    {
        // ⛔ THE BOUNDARY OF THE RULE. The prefix PARSES — it names 5 bytes and the file holds them — but 5 is
        // below the reader's VaryMin of 20. §9.1.13.2 item 3 makes that a SUCCESSFUL READ with '04' ("shorter
        // than or longer than the minimum or maximum length of records allowed for the fixed file attributes
        // for that file"), which a '39' at the OPEN would make unreachable. The OPEN must succeed.
        string host = Host("ffa-vary-bounds.dat");
        CobolFile.Init();
        CobolFile.Register("WR", host, 40, lineSequential: false, optional: false, varyMin: 1, varyMax: 40);
        CobolFile.OpenOutput("WR", host, assignDynamic: false, page: null);
        CobolFile.Write("WR", "HELLO", 5, page: null);
        CobolFile.Close("WR");

        CobolFile.Init();
        CobolFile.Register("RD", host, 40, lineSequential: false, optional: false, varyMin: 20, varyMax: 40);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    [Fact]
    public void AFixedLengthSequentialConnectorNeverAsksAboutTheFraming()
    {
        // The guard on the arm above: the parse test applies to RECORD VARYING record-sequential files ALONE.
        // A fixed-length connector over the same bytes reads them as plain data, which is what §9.1.7.2 says a
        // record sequential file's fixed-length form is.
        string host = Host("ffa-vary-fixedreader.dat");
        CobolFile.Init();
        CobolFile.Register("WR", host, 40, lineSequential: false, optional: false, varyMin: 5, varyMax: 40);
        CobolFile.OpenOutput("WR", host, assignDynamic: false, page: null);
        CobolFile.Write("WR", "HELLO", 5, page: null);
        CobolFile.Close("WR");

        CobolFile.Init();
        CobolFile.Register("RD", host, 9, lineSequential: false, optional: false);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    // ── The INDEXED key table, and its documented runtime opt-out ────────────────────────────────────────────

    /// <summary>Create an indexed file with a 4-byte prime key and one 5-byte alternate.</summary>
    private static void MakeIndexedFile(string host)
    {
        CobolFile.Init();
        CobolFile.RegisterIndexed("IX", host, 20, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("IX", 4, 5, duplicates: true, suppress: "ZZZZZ");
        CobolFile.OpenOutput("IX", host, assignDynamic: false, page: null);
        CobolFile.Close("IX");
    }

    [Fact]
    public void IndexedKeyGeometryRoundTripsThroughTheStoreHeader()
    {
        string host = Host("ffa-ix.dat");
        MakeIndexedFile(host);

        // The header is what a later run reads back, so the round trip is asserted through the comparison the
        // OPEN performs: the same description matches, and each single-attribute change is a '39'.
        CobolFile.Init();
        CobolFile.RegisterIndexed("RD", host, 20, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("RD", 4, 5, duplicates: true, suppress: "ZZZZZ");
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    [Theory]
    // §12.4.5.12.4 GR3 — the PRIME key's window ("shall be the same as that used when the file was created").
    [InlineData(0, 6, true, "ZZZZZ")]
    // §12.4.5.6.4 GR3 — an ALTERNATE key's window, its DUPLICATES phrase, and its §12.4.5.6.4 GR6 SUPPRESS
    // WHEN value; each on its own, so no one of them can be riding another.
    [InlineData(0, 4, false, "ZZZZZ")]
    [InlineData(0, 4, true, "YYYYY")]
    public void AnIndexedKeyTableDisagreementIsAConflict(int primeOff, int primeLen, bool dups, string suppress)
    {
        string host = Host($"ffa-ix-{primeOff}-{primeLen}-{dups}-{suppress}.dat");
        MakeIndexedFile(host);

        CobolFile.Init();
        CobolFile.RegisterIndexed("RD", host, 20, false, 0, primeOff, primeLen);
        CobolFile.AddAlternateKey("RD", 4, 5, dups, suppress: suppress);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void TheNumberOfAlternateKeysIsItsOwnRequirement()
    {
        // §12.4.5.6.4 GR3's second sentence: "The number of alternate record keys for the file shall also be
        // the same as that used when the physical file was created." A description that simply omits the
        // alternate — every key it DOES declare agreeing — is still the conflict.
        // ⚖ Behaviourally GnuCOBOL's "INDEXED undeclared keys" case.
        string host = Host("ffa-ix-count.dat");
        MakeIndexedFile(host);

        CobolFile.Init();
        CobolFile.RegisterIndexed("RD", host, 20, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void TheKeyCheckOptOutOpensTheSameFile_AndNarrowsNothingElse()
    {
        // ⛔ The documented runtime opt-out — COBOLNET_KEYCHECK=OFF, GnuCOBOL's COB_KEYCHECK=OFF under this
        // repository's naming (docs/CONFORMANCE.md DOC-A.1-129). No COBOL program can set it, so no golden can
        // reach it. It is read ONCE per run unit, in FileRegistry's constructor and its Reset — which is why
        // CobolFile.Init() below is load-bearing rather than hygiene.
        string host = Host("ffa-ix-keycheck.dat");
        MakeIndexedFile(host);

        Environment.SetEnvironmentVariable(FileRegistry.KeyCheckVariable, "OFF");
        CobolFile.Init();
        CobolFile.RegisterIndexed("RD", host, 20, false, 0, primeOffset: 0, primeLength: 6);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");

        // ⛔ IT NARROWS THE SET BY EXACTLY THE KEY TABLE. The organization, the record type and the record
        // sizes stay validated — an opt-out that switched the whole comparison off would pass the assertion
        // above for the wrong reason.
        CobolFile.Init();
        CobolFile.RegisterIndexed("SZ", host, 44, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("SZ", 4, 5, duplicates: true, suppress: "ZZZZZ");
        CobolFile.OpenInput("SZ", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("SZ"));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("on", true)]
    [InlineData("OFF", false)]
    [InlineData(" off ", false)]
    [InlineData("0", false)]
    [InlineData("False", false)]
    [InlineData("no", false)]
    public void TheKeyCheckVariablesVocabulary(string? value, bool expected)
    {
        Environment.SetEnvironmentVariable(FileRegistry.KeyCheckVariable, value);
        Assert.Equal(expected, new FileRegistry().KeyCheck);
    }

    [Fact]
    public void TheKeyCheckVariableIsRegisteredInTheRuntimeConfig()
    {
        // RuntimeConfig is the ONE answer to "which environment variables does a WiseOwl COBOL program honor".
        var entry = RuntimeConfig.Find(FileRegistry.KeyCheckVariable);
        Assert.NotNull(entry);
        Assert.Equal("files", entry!.Subsystem);
        // ⛔ COBOLNET_, never COBOL_: SwitchStore.Prefix is COBOL_, so a COBOL_-named variable would be
        // swallowed by the §12.3.7 external-switch FAMILY entry instead of getting its own.
        Assert.StartsWith("COBOLNET_", FileRegistry.KeyCheckVariable, StringComparison.Ordinal);
        Assert.False(entry.IsPattern);
    }

    [Fact]
    public void ASuppressValueCarryingArbitraryBytesRoundTrips()
    {
        // §12.4.5.6.4 GR6 admits any figurative-constant or literal as the SUPPRESS WHEN value, including one
        // holding a NUL or a line ending. The header stores it with an explicit LENGTH rather than a delimiter,
        // so a legal COBOL program cannot write a store its own next OPEN refuses.
        string host = Host("ffa-suppress.dat");
        CobolFile.Init();
        CobolFile.RegisterIndexed("IX", host, 12, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("IX", 4, 4, duplicates: false, suppress: "a,\r\n\0b");
        CobolFile.OpenOutput("IX", host, assignDynamic: false, page: null);
        CobolFile.Close("IX");

        CobolFile.Init();
        CobolFile.RegisterIndexed("RD", host, 12, false, 0, primeOffset: 0, primeLength: 4);
        CobolFile.AddAlternateKey("RD", 4, 4, duplicates: false, suppress: "a,\r\n\0b");
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("RD"));
        CobolFile.Close("RD");
    }

    // ── §9.1.6's "at the time it is created" — the two moments the OPEN statement creates a file ─────────────

    [Fact]
    public void OpenOutputReestablishesTheAttributes_GR18()
    {
        string host = Host("ffa-recreate.dat");
        MakeRelativeTenByteFile(host);

        // §14.9.27.4 GR18: "If the OUTPUT phrase is specified, the successful execution of the OPEN statement
        // creates the file" — and §9.1.6 fixes the attributes at creation. So OPEN OUTPUT is never judged
        // against the previous file's attributes; it replaces them, in the store header it writes.
        CobolFile.Init();
        CobolFile.RegisterRelative("WR", host, 40, false, 0, 0);
        CobolFile.OpenOutput("WR", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.Success, CobolFile.Status("WR"));
        CobolFile.Close("WR");

        // ... the connector that conflicted before now matches ...
        Assert.Equal(FileStatusCode.Success, OpenContradictingRelativeConnector(host));

        // ... and the one that matched before now conflicts.
        CobolFile.Init();
        CobolFile.RegisterRelative("RD", host, 10, false, 0, 0);
        CobolFile.OpenInput("RD", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.FixedAttributeConflict, CobolFile.Status("RD"));
    }

    [Fact]
    public void AnAbsentOptionalFileCreatedByOpenExtend_RecordsItsAttributes_GR17()
    {
        // §14.9.27.4 GR17: an absent OPTIONAL file opened EXTEND (or I-O) is created "as if OPEN OUTPUT /
        // CLOSE" were executed — the other moment §9.1.6's "at the time it is created" names. The subject is
        // RELATIVE because a sequential file records nothing that could prove the creation happened.
        string host = Host("ffa-optional.dat");
        CobolFile.Init();
        CobolFile.RegisterRelative("OP", host, 12, optional: true, accessMode: 0, relativeKeyDigits: 0);
        CobolFile.OpenExtend("OP", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.Status("OP"));   // '05' GR17
        CobolFile.Close("OP");

        Assert.True(File.Exists(host));
        Assert.Equal(FileStatusCode.FixedAttributeConflict, OpenContradictingRelativeConnector(host));
    }

    [Fact]
    public void AnAbsentOptionalFileOpenedInput_IsNotCreated_AndRecordsNothing()
    {
        // Table 18: an OPTIONAL file opened INPUT while unavailable is a normal open whose first read is the at
        // end condition — the file is NOT created, so there is nothing whose attributes could be fixed.
        string host = Host("ffa-absent.dat");
        CobolFile.Init();
        CobolFile.RegisterRelative("AB", host, 12, optional: true, accessMode: 0, relativeKeyDigits: 0);
        CobolFile.OpenInput("AB", host, assignDynamic: false, page: null);
        Assert.Equal(FileStatusCode.OptionalFileNotFound, CobolFile.Status("AB"));
        CobolFile.Close("AB");
        Assert.Empty(Directory.GetFiles(_dir));
    }

    // ── The per-organization dispatch cannot silently lose an arm ────────────────────────────────────────────

    [Fact]
    public void EveryOrganizationStatesWhatItsOwnFormatEncodes()
    {
        // ⛔ THE DRIFT GUARD ON THE ANNEX A.1 ITEM 129 RULE. The validated set is "exactly what each
        // organization's format encodes", and that is realized as overrides of FileConnector's ONE virtual
        // rather than as a table — so the property that must not drift is that every organization whose format
        // DOES encode something declares its own answer. SequentialConnector (the RECORD VARYING prefix) and
        // KeyedConnector (the store header) do; RelativeConnector and IndexedConnector inherit KeyedConnector's
        // deliberately, because they share the format and differ only in DeclaredKeys.
        //
        // The base answer is "no conflict", so an organization added WITHOUT an override silently validates
        // nothing — which is exactly the failure this asserts against, and it is why the assertion names the
        // declaring types rather than merely calling the method.
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        static bool Declares(Type t) => t.GetMethod("FixedAttributeConflict", Flags) is not null;

        Assert.True(Declares(typeof(FileConnector)), "the base states the 'this format encodes nothing' answer");
        Assert.True(Declares(typeof(SequentialConnector)), "§9.1.7.2's record-length header is a sequential answer");
        Assert.True(Declares(typeof(KeyedConnector)), "the framed store's header is the keyed answer");

        // And the base's answer really is "no conflict" — a guess there would manufacture a '39' for a file
        // that stated nothing.
        Assert.Equal([typeof(SequentialConnector), typeof(KeyedConnector)],
            typeof(FileConnector).Assembly.GetTypes()
                .Where(t => t != typeof(FileConnector) && typeof(FileConnector).IsAssignableFrom(t) && Declares(t))
                .OrderBy(t => t.Name == nameof(KeyedConnector)).ToArray());
    }

    [Fact]
    public void DeclaredAttributesNamesEachConnectorsOwnOrganization()
    {
        // ⛔ A DEAD LOOKUP IS ALSO UNVERIFIED. FileConnector.DeclaredAttributes is §14.9.27.4 GR10's own phrase
        // — "the attributes of the file connector as specified in the file control paragraph and the file
        // description entry" — and GR10 states it for every organization, so it stays on the base. But since the
        // sequential formats record nothing, the OPEN path consults it only for RELATIVE and INDEXED, and
        // SequentialConnector's DeclaredOrganization would otherwise be a value nothing had ever contradicted.
        // This asserts it directly, so the day a sequential format DOES record something the value it will be
        // compared against is already known to be right.
        Assert.Equal(FixedFileAttributes.Sequential,
            new SequentialConnector("s.dat", 20, lineSequential: false).DeclaredAttributes.Organization);
        Assert.Equal(FixedFileAttributes.Sequential,
            new SequentialConnector("l.dat", 20, lineSequential: true).DeclaredAttributes.Organization);
        Assert.Equal(FixedFileAttributes.Relative,
            new RelativeConnector("r.dat", 20, KeyedAccess.Sequential, 0).DeclaredAttributes.Organization);
        Assert.Equal(FixedFileAttributes.Indexed,
            new IndexedConnector("i.dat", 20, KeyedAccess.Sequential, 0, 4).DeclaredAttributes.Organization);

        // §9.1.6 names exactly three organizations — "There are three organizations: sequential, relative, and
        // indexed" — so those three constants are the whole vocabulary, and each is distinct.
        Assert.Equal(3, new HashSet<string>(
            [FixedFileAttributes.Sequential, FixedFileAttributes.Relative, FixedFileAttributes.Indexed]).Count);

        // The RECORD clause half, also assembled once on the base for every organization (§13.18.43 GR9/GR10):
        // a fixed connector reports its width for both bounds, a varying one its declared bounds.
        var fixedRec = new RelativeConnector("f.dat", 20, KeyedAccess.Sequential, 0).DeclaredAttributes;
        Assert.False(fixedRec.Varying);
        Assert.Equal((20, 20), (fixedRec.MinRecordSize, fixedRec.MaxRecordSize));
        var varyRec = new RelativeConnector("v.dat", 40, KeyedAccess.Sequential, 0, varyMin: 5, varyMax: 40)
            .DeclaredAttributes;
        Assert.True(varyRec.Varying);
        Assert.Equal((5, 40), (varyRec.MinRecordSize, varyRec.MaxRecordSize));
    }

    [Fact]
    public void FingerprintIsNativeForNoSequence_AndIsStable()
    {
        Assert.Equal("NATIVE", FixedFileAttributes.Fingerprint(null));
        // A sequence's identity is its WEIGHTS, so the same sequence fingerprints identically every time and a
        // reordered one does not (§9.1.6 — the collating sequence of the keys is a fixed file attribute).
        var a = OrdinalSequence(swapAB: false);
        var b = OrdinalSequence(swapAB: false);
        var c = OrdinalSequence(swapAB: true);
        Assert.Equal(FixedFileAttributes.Fingerprint(a), FixedFileAttributes.Fingerprint(b));
        Assert.NotEqual(FixedFileAttributes.Fingerprint(a), FixedFileAttributes.Fingerprint(c));
        Assert.NotEqual("NATIVE", FixedFileAttributes.Fingerprint(c));
    }

    /// <summary>An ALPHABET-shaped sequence over the native ordinal positions (§12.3.7.4 GR7), optionally with
    /// the positions of 'A' and 'B' exchanged — two sequences that order key values differently.</summary>
    private static AlphanumericCollation OrdinalSequence(bool swapAB)
    {
        // The SPARSE §12.3.7.4 GR7 k table (kb/Work PB770): specify codes 0..255 at their own positions, so the
        // sequence IS the native order except for the optional 'A'/'B' exchange; everything above follows natively.
        var codes = Enumerable.Range(0, 256).Select(c => (ushort)c).ToArray();
        var positions = Enumerable.Range(0, 256).Select(c => (ushort)c).ToArray();
        var repByPos = Enumerable.Range(0, 256).Select(c => (ushort)c).ToArray();
        if (swapAB)
        {
            (positions['A'], positions['B']) = (positions['B'], positions['A']);
            (repByPos['A'], repByPos['B']) = (repByPos['B'], repByPos['A']);
        }
        return new AlphanumericCollation(codes, positions, repByPos, 256);
    }
}
