// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A READ DELIVERS THE RECORD THE PHYSICAL FILE HOLDS NOW, NEVER A BUFFERED IMAGE A SIBLING CONNECTOR HAS
/// ALREADY REPLACED (kb/Work PB753) — the READ-side twin of <see cref="SharedExtendWriteDriftTests"/>'s
/// write-side rule.
///
/// <para><b>The derivation.</b> §14.9.35.4 GR4: <i>"The successful execution of the REWRITE statement releases a
/// logical record to the operating environment."</i> §14.9.51.4 GR12 says the same of WRITE. §9.1.15 3) makes
/// the concurrency legal: <i>"The sharing with all other mode allows concurrent access to a physical file
/// through other file connectors specifying input, I-O, or extend mode."</i> And §14.9.30.4 GR21 c)/d) say what
/// the reader then owes — the record selected is <i>"the first existing record in the physical file whose
/// relative key number is greater than the file position indicator if NEXT is specified or implied"</i>, and
/// that record <i>"is made available in the record area associated with file-name-1"</i>. The physical file, at
/// the READ. The standard has no rule about buffering because the read-ahead is entirely ours.</para>
///
/// <para><b>What PB753 was.</b> The sequential connector reads through a <see cref="StreamReader"/> over a
/// buffered <see cref="FileStream"/> — two buffers. A sibling connector's REWRITE reached the file and reported
/// '00', and the reader went on serving its snapshot: measured over 300 four-byte records, <c>R-3=OLD</c> while
/// the file on disk held <c>NEW</c>, with '00' from every statement. The invalidation rule was already written
/// down in <c>SeekToRecord</c>'s doc comment — <i>"the reader buffers ahead of the base stream, so seeking the
/// stream alone would keep serving stale characters"</i> — for the connector's OWN seek and never for a
/// sibling's write: one rule, one arm.</para>
///
/// <para><b>So the guard is the rule, not the reported arm:</b> a shared reader may serve characters only from
/// a buffer that agrees with the physical file's release generation, and only ONE buffer may stand between the
/// connector and the medium. Both halves are needed and each is measured here: the generation alone left the
/// <see cref="FileStream"/>'s own 4096-byte buffer serving the superseded bytes (<see cref="FileStream.Seek"/>
/// reuses its read buffer when the target lands inside it), and an unbuffered handle alone would leave the
/// <see cref="StreamReader"/>'s 1024 characters just as stale.</para>
///
/// <para>The matrix is deliberately not the reported case: every sequential framing, a buffer-crossing AND a
/// sub-buffer record count, reader-first AND writer-first, the clause-less pair kb/Work PB740 admitted, plus the
/// controls that must stay green — the sibling APPEND, the lone I-O connector rewriting its own records, and the
/// keyed organizations, which were already right because their medium is the ONE shared store.</para>
/// </summary>
public sealed class SharedReadCoherenceDriftTests
{
    private static string Tmp(string tag) =>
        Path.Combine(Path.GetTempPath(), $"pb753-{tag}-{Guid.NewGuid():N}.dat");

    private static void TryDelete(string host)
    {
        // ONE artifact per physical file (kb/Work PB802): a keyed file's §9.1.6 fixed file attributes
        // are the store header INSIDE it, so deleting the data file deletes them.
        try { File.Delete(host); } catch (IOException) { }
    }

    /// <summary>The sharing spellings a READER can wear. The two FILE-CONTROL spellings come from
    /// <see cref="SharedExtendOpenDriftTests.Spelling"/> so the matrices cannot drift apart; the third is the
    /// one this note added — NO clause anywhere (kb/Work PB740), which reaches the read-coherence rule on its
    /// own connector's medium even though no sibling can be open beside it (kb/Work PB322).</summary>
    public enum ReaderSpelling
    {
        /// <summary>SHARING WITH ALL OTHER + LOCK MODE IS MANUAL.</summary>
        AllOtherClause,

        /// <summary>A LOCK MODE clause and no SHARING clause — §9.1.15's implementor default of the open mode.</summary>
        LockModeOnly,

        /// <summary>Neither clause. <c>FileConnector.SharedPhysical</c> is NULL for such a connector (it sets no
        /// record locks — §12.4.5.9.4 GR1 b) 2.), which is exactly why the release generation had to be reachable
        /// through <c>FileConnector.Physical</c> instead: the read-coherence rule is a rule about the medium, not
        /// about record locking (kb/Work PB740 widened the reach, PB753 covers it).</summary>
        NoClause,
    }

    public static TheoryData<SharedExtendOpenDriftTests.Framing, ReaderSpelling> Shapes()
    {
        var data = new TheoryData<SharedExtendOpenDriftTests.Framing, ReaderSpelling>();
        foreach (var f in Enum.GetValues<SharedExtendOpenDriftTests.Framing>())
            foreach (var s in Enum.GetValues<ReaderSpelling>())
                data.Add(f, s);
        return data;
    }

    /// <summary>The shapes in which a SIBLING connector is open on the same physical file at the same time as the
    /// reader. Only a connector that declared SHARING WITH ALL OTHER can be: since kb/Work PB322 a connector with
    /// no SHARING clause is SHARING WITH READ ONLY when it opens INPUT and SHARING WITH NO OTHER in every other
    /// mode (<c>FileRegistry.ImplementorDefaultSharing</c>), and Table 19 refuses a rewriting or appending
    /// sibling next to either — so the clause-less spellings are measured only where no sibling exists
    /// (<see cref="Shapes"/>).</summary>
    public static TheoryData<SharedExtendOpenDriftTests.Framing, ReaderSpelling> SiblingShapes()
    {
        var data = new TheoryData<SharedExtendOpenDriftTests.Framing, ReaderSpelling>();
        foreach (var f in Enum.GetValues<SharedExtendOpenDriftTests.Framing>())
            data.Add(f, ReaderSpelling.AllOtherClause);
        return data;
    }

    public static TheoryData<SharedExtendOpenDriftTests.Framing, int> Sizes()
    {
        var data = new TheoryData<SharedExtendOpenDriftTests.Framing, int>();
        foreach (var f in Enum.GetValues<SharedExtendOpenDriftTests.Framing>())
        {
            data.Add(f, 5);      // the whole file inside one StreamReader buffer
            data.Add(f, 400);    // 1600 bytes — the read-ahead cannot hold it, so a refill happens mid-file
        }
        return data;
    }

    private static void Share(FileRegistry reg, string name, ReaderSpelling spelling)
    {
        switch (spelling)
        {
            case ReaderSpelling.AllOtherClause:
                reg.RegisterSharing(name, FileSharing.AllOther, FileLockMode.Manual, multiple: false);
                break;
            case ReaderSpelling.LockModeOnly:
                reg.RegisterSharing(name, null, FileLockMode.Manual, multiple: false);
                break;
            case ReaderSpelling.NoClause:
                break;   // the point of this arm: nothing is registered at all
        }
    }

    private static void RegisterSequential(FileRegistry reg, string name, string host,
        SharedExtendOpenDriftTests.Framing framing) =>
        reg.Register(name, host, recordWidth: 4,
            lineSequential: framing == SharedExtendOpenDriftTests.Framing.LineSequential,
            optional: false,
            varyMin: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 4 : -1,
            varyMax: framing == SharedExtendOpenDriftTests.Framing.VaryingRecordSequential ? 8 : -1);

    private static string Read(FileRegistry reg, string name, out string image) =>
        reg.ReadShared(name, false, FileRecordLock.None, false, false, FileRetryKind.None, 0, out image);

    private static string Rewrite(FileRegistry reg, string name, string image) =>
        reg.RewriteShared(name, image, -1, FileRecordLock.None, FileRetryKind.None, 0);

    /// <summary>Four printable characters per record, so LINE SEQUENTIAL's trailing-space trim cannot make the
    /// physical span depend on the value (§14.9.35.4 GR17 c) rewrites within the replaced record's span).</summary>
    private static string Rec(int n) => $"R{n % 1000:000}";

    private static void Seed(FileRegistry reg, string host, SharedExtendOpenDriftTests.Framing framing, int count)
    {
        RegisterSequential(reg, "SEED", host, framing);
        reg.OpenStatic("SEED", FileOpenMode.Output);
        for (int i = 1; i <= count; i++)
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("SEED", Rec(i), -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
        reg.Close("SEED");
    }

    // ── THE DEFECT: a sibling's REWRITE, over every framing and every sharing spelling ────────────────────────

    /// <summary>The reported shape, generalised. A reader that has already taken record 1 (and therefore filled
    /// its read-ahead) must see the record a sibling I-O connector REWRITEs at a later ordinal — §14.9.35.4 GR4
    /// put it in the physical file and §14.9.30.4 GR21 c)/d) select it FROM the physical file.
    /// <para>The rewritten ordinal is inside the reader's read-ahead in every framing, which is what makes the
    /// measurement about the buffer and not about the file.</para>
    /// <para>⛔ AND THE SEQUENCE IS NOT THE REPORTED ONE, BECAUSE THE REPORTED ONE CANNOT MEASURE THIS IN EVERY
    /// SPELLING. <c>FileRegistry.SyncHostPostures</c> calls <c>FileConnector.Reposture</c> on every OTHER
    /// connector whenever the §9.1.15 union over the physical file CHANGES, and the sequential connector
    /// implements a reposture by REBUILDING its handle at the logical offset — which throws the read-ahead away
    /// as a side effect. For <see cref="ReaderSpelling.AllOtherClause"/> the union is already
    /// <c>FileShare.ReadWrite</c> and nothing moves, but for the two default spellings it WOULD widen at
    /// the sibling's OPEN and narrow again at its CLOSE, so a probe that fills the buffer before the OPEN and
    /// reads after the CLOSE is rescued twice by an accident and passes with the rule deleted. Measured, not
    /// argued: with <c>EnsureReaderCoherent</c> injected out, the reported sequence went red on 3 of these 9
    /// cases and green on the 6 that Reposture rescued. So the reader takes ONE MORE record after the sibling's
    /// open — re-filling the read-ahead under the final handle — and reads again while the sibling is still
    /// OPEN, which leaves no reposture between the fill and the read in any spelling
    /// (<c>feedback_probe_the_shape_the_subject_hides</c>). All 9 are red with the rule removed.</para></summary>
    [Theory]
    [MemberData(nameof(SiblingShapes))]
    public void ASiblingsRewriteIsVisibleToAnAlreadyReadingConnector(
        SharedExtendOpenDriftTests.Framing framing, ReaderSpelling spelling)
    {
        string host = Tmp($"rw-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            Seed(reg, host, framing, 400);

            RegisterSequential(reg, "A", host, framing);
            RegisterSequential(reg, "B", host, framing);
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);

            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string first));
            Assert.Equal(Rec(1), first.TrimEnd());

            reg.OpenStatic("B", FileOpenMode.IO);

            // The re-fill under the FINAL handle: whatever the sibling's open did to this reader's posture has
            // happened, so the characters below are read-ahead the release must invalidate and not leftovers a
            // rebuild would have dropped anyway.
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string second));
            Assert.Equal(Rec(2), second.TrimEnd());

            for (int i = 1; i <= 4; i++)
                Assert.Equal(FileStatusCode.Success, Read(reg, "B", out _));
            Assert.Equal(FileStatusCode.Success, Rewrite(reg, "B", "NEWX"));   // ordinal 4

            // …and the reader reads on with the sibling STILL OPEN, so no closing reposture can rescue it.
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string third));
            Assert.Equal(Rec(3), third.TrimEnd());
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string fourth));
            Assert.Equal("NEWX", fourth.TrimEnd());
            reg.Close("B");
            reg.Close("A");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The axis the reported case held fixed: the record count. A file WHOLLY inside one read-ahead
    /// and a file several read-aheads long are different physical situations — the first never refills, the
    /// second refills mid-file — and the rule is the same for both
    /// (<c>feedback_probe_the_shape_the_subject_hides</c>).
    /// <para>This is also the leg that keeps the CLOSE-FIRST ordering measured: the sibling closes before the
    /// reader reads again, which is the shape the defect was reported in. It is pinned to
    /// <see cref="ReaderSpelling.AllOtherClause"/> for the reason
    /// <see cref="ASiblingsRewriteIsVisibleToAnAlreadyReadingConnector"/> gives — that spelling's §9.1.15 union
    /// is already <c>FileShare.ReadWrite</c>, so no reposture fires at the sibling's open or close and the
    /// stale buffer is still there to be caught.</para></summary>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void TheRuleHoldsWhetherOrNotTheFileFitsInTheReadAhead(
        SharedExtendOpenDriftTests.Framing framing, int count)
    {
        string host = Tmp($"size-{framing}-{count}");
        try
        {
            var reg = new FileRegistry();
            Seed(reg, host, framing, count);

            RegisterSequential(reg, "A", host, framing);
            RegisterSequential(reg, "B", host, framing);
            Share(reg, "A", ReaderSpelling.AllOtherClause);
            Share(reg, "B", ReaderSpelling.AllOtherClause);

            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out _));
            reg.OpenStatic("B", FileOpenMode.IO);
            for (int i = 1; i <= 3; i++)
                Assert.Equal(FileStatusCode.Success, Read(reg, "B", out _));
            Assert.Equal(FileStatusCode.Success, Rewrite(reg, "B", "NEWX"));
            reg.Close("B");

            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out _));                  // ordinal 2
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string third));       // ordinal 3
            Assert.Equal("NEWX", third.TrimEnd());
            reg.Close("A");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The other ordering: the WRITER opens first and the reader second, so the reader's handle was
    /// created under the widened posture from the start rather than repostured into it. The reader still has to
    /// re-anchor, because it fills its read-ahead before the REWRITE happens.</summary>
    [Theory]
    [MemberData(nameof(SiblingShapes))]
    public void TheRuleHoldsWhenTheWriterOpensFirst(
        SharedExtendOpenDriftTests.Framing framing, ReaderSpelling spelling)
    {
        string host = Tmp($"wfirst-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            Seed(reg, host, framing, 400);

            RegisterSequential(reg, "A", host, framing);
            RegisterSequential(reg, "B", host, framing);
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);

            reg.OpenStatic("B", FileOpenMode.IO);
            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out _));   // fills the read-ahead

            for (int i = 1; i <= 3; i++)
                Assert.Equal(FileStatusCode.Success, Read(reg, "B", out _));
            Assert.Equal(FileStatusCode.Success, Rewrite(reg, "B", "NEWX"));

            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out _));
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string third));
            Assert.Equal("NEWX", third.TrimEnd());
            reg.Close("A");
            reg.Close("B");
        }
        finally { TryDelete(host); }
    }

    // ── CONTROLS: the arms that were already right and shall stay right ───────────────────────────────────────

    /// <summary>The APPEND control, measured on the PB739 tree and recorded in the note so a later reader does
    /// not widen this defect to a shape that was checked and is correct: a record a sibling adds after the
    /// reader has consumed everything IS delivered — a reader can only have buffered what existed. Re-measured
    /// here because the invalidation now fires on an append too (§14.9.51.4 GR12 is a release), and firing
    /// must not cost the record.</summary>
    [Theory]
    [MemberData(nameof(SiblingShapes))]
    public void ASiblingsAppendedRecordIsStillDelivered(
        SharedExtendOpenDriftTests.Framing framing, ReaderSpelling spelling)
    {
        string host = Tmp($"app-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            Seed(reg, host, framing, 2);

            RegisterSequential(reg, "A", host, framing);
            RegisterSequential(reg, "B", host, framing);
            Share(reg, "A", spelling);
            Share(reg, "B", spelling);

            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out _));
            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out _));   // the whole file consumed

            reg.OpenStatic("B", FileOpenMode.Extend);
            Assert.Equal(FileStatusCode.Success,
                reg.WriteShared("B", "APPD", -1, FileRecordLock.None, FileRetryKind.None, 0, page: null));
            reg.Close("B");

            Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string appended));
            Assert.Equal("APPD", appended.TrimEnd());
            reg.Close("A");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The SELF control. One I-O connector reading and rewriting its own records is not a sharing
    /// situation at all: a sequential REWRITE targets the record the last READ delivered, whose bytes are at or
    /// before the file position indicator, so nothing this connector will read again was touched. The records
    /// after the rewritten one must come back unchanged — and the connector's own release must not be treated
    /// as a sibling's, or a READ/REWRITE loop would re-fill its buffer once per record.</summary>
    [Theory]
    [MemberData(nameof(Shapes))]
    public void AConnectorsOwnRewriteDoesNotDisturbItsOwnForwardRead(
        SharedExtendOpenDriftTests.Framing framing, ReaderSpelling spelling)
    {
        string host = Tmp($"self-{framing}-{spelling}");
        try
        {
            var reg = new FileRegistry();
            Seed(reg, host, framing, 6);

            RegisterSequential(reg, "A", host, framing);
            Share(reg, "A", spelling);
            reg.OpenStatic("A", FileOpenMode.IO);
            for (int i = 1; i <= 6; i++)
            {
                Assert.Equal(FileStatusCode.Success, Read(reg, "A", out string got));
                Assert.Equal(Rec(i), got.TrimEnd());
                Assert.Equal(FileStatusCode.Success, Rewrite(reg, "A", $"X{i:000}"));
            }
            reg.Close("A");

            RegisterSequential(reg, "R", host, framing);
            reg.OpenStatic("R", FileOpenMode.Input);
            for (int i = 1; i <= 6; i++)
            {
                Assert.Equal(FileStatusCode.Success, Read(reg, "R", out string got));
                Assert.Equal($"X{i:000}", got.TrimEnd());
            }
            reg.Close("R");
        }
        finally { TryDelete(host); }
    }

    /// <summary>The KEYED control, and the argument that named the fix. <c>RelativeConnector</c> and
    /// <c>IndexedConnector</c> do not read the host file per operation — they attach to the ONE
    /// <c>KeyedStoreTable</c> store for the path (kb/Work PB143), so a sibling's REWRITE is visible to every
    /// attached connector the instant it happens. That is a release generation, already built, for the other two
    /// organizations; the sequential connector's shared medium is the file system, and PB753 gave it the same
    /// property. Measured, not assumed.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheKeyedOrganizationsSeeASiblingsRewriteImmediately(bool indexed)
    {
        string host = Tmp(indexed ? "idx" : "rel");
        try
        {
            var reg = new FileRegistry();
            void RegisterKeyed(string name)
            {
                if (indexed)
                    reg.RegisterIndexed(name, host, 8, optional: false, accessMode: 0, primeOffset: 0,
                        primeLength: 4, -1, -1);
                else
                    reg.RegisterRelative(name, host, 4, optional: false, accessMode: 0, relativeKeyDigits: 4,
                        -1, -1);
            }
            string Image(int i, string val) => indexed ? $"K{i:000}{val}" : val;

            RegisterKeyed("SEED");
            reg.OpenStatic("SEED", FileOpenMode.Output);
            for (int i = 1; i <= 4; i++)
                Assert.Equal(FileStatusCode.Success,
                    reg.WriteShared("SEED", Image(i, Rec(i)), -1, FileRecordLock.None, FileRetryKind.None, 0,
                        page: null));
            reg.Close("SEED");

            RegisterKeyed("A");
            RegisterKeyed("B");
            Share(reg, "A", ReaderSpelling.AllOtherClause);
            Share(reg, "B", ReaderSpelling.AllOtherClause);

            reg.OpenStatic("A", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.Success, reg.ReadKeyedNext("A", out _));

            reg.OpenStatic("B", FileOpenMode.IO);
            for (int i = 1; i <= 3; i++)
                Assert.Equal(FileStatusCode.Success, reg.ReadKeyedNext("B", out _));
            Assert.Equal(FileStatusCode.Success, Rewrite(reg, "B", Image(3, "NEWX")));
            reg.Close("B");

            Assert.Equal(FileStatusCode.Success, reg.ReadKeyedNext("A", out _));
            Assert.Equal(FileStatusCode.Success, reg.ReadKeyedNext("A", out string third));
            Assert.Equal(Image(3, "NEWX"), third.TrimEnd());
            reg.Close("A");
        }
        finally { TryDelete(host); }
    }

    // ── THE STRUCTURE, so "automatic" stays true ──────────────────────────────────────────────────────────────

    private static string ConnectorSource =>
        TestRepo.Src("Cobol.Net.Runtime", "IO", "SequentialConnector.cs");

    /// <summary>The line range of a member's body, by brace matching from its declaration. Comment text is
    /// ignored so a brace inside a doc comment cannot move the boundary.</summary>
    private static (int Start, int End) Body(string[] lines, string signature)
    {
        int decl = Array.FindIndex(lines, l => l.Contains(signature, StringComparison.Ordinal)
            && !l.TrimStart().StartsWith("//", StringComparison.Ordinal));
        Assert.True(decl >= 0, $"'{signature}' is no longer declared in SequentialConnector.cs.");
        int depth = 0;
        for (int i = decl; i < lines.Length; i++)
        {
            string code = Strip(lines[i]);
            foreach (char ch in code)
            {
                if (ch == '{') depth++;
                else if (ch == '}' && --depth == 0) return (decl, i);
            }
        }
        Assert.Fail($"'{signature}' has no matching close brace.");
        return (0, 0);
    }

    private static string Strip(string line)
    {
        string t = line.TrimStart();
        if (t.StartsWith("//", StringComparison.Ordinal)) return "";
        int c = line.IndexOf("//", StringComparison.Ordinal);
        return c < 0 ? line : line[..c];
    }

    /// <summary>⛔ THE ONE WALK, AND THE CHECK AT ITS HEAD. Every character this connector ever reads comes out
    /// of <c>FillChars</c> or <c>ReadPhysicalLine</c>, and both are reached only from <c>NextFrame</c>, whose
    /// first statement is <c>EnsureReaderCoherent()</c>. That is what makes the invalidation a property of the
    /// connector rather than a courtesy at a call site — a fourth framing, or a second reader loop, cannot be
    /// added without either passing the check or failing here (kb/Work PB753, and
    /// <c>feedback_two_arm_dispatch</c>: the rule already existed in <c>SeekToRecord</c> for ONE arm).</summary>
    [Fact]
    public void EveryPhysicalReadGoesThroughTheOneWalkAndItsCoherenceCheck()
    {
        string[] lines = File.ReadAllLines(ConnectorSource);
        var fill = Body(lines, "private int FillChars(");
        var line = Body(lines, "private string? ReadPhysicalLine(");
        var frame = Body(lines, "private string? NextFrame(");

        // 1. The check is the first statement of the walk.
        int firstStatement = -1;
        for (int i = frame.Start; i <= frame.End; i++)
        {
            string code = Strip(lines[i]).Trim();
            if (code.Length == 0 || code == "{" || code.EndsWith(')') && i == frame.Start) continue;
            if (code == "}") continue;
            firstStatement = i;
            break;
        }
        Assert.True(firstStatement > 0, "NextFrame has no statements.");
        Assert.Contains("EnsureReaderCoherent();", Strip(lines[firstStatement]), StringComparison.Ordinal);

        // 2. Nothing else reads characters out of the reader.
        var strays = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string code = Strip(lines[i]);
            if (!code.Contains("_reader", StringComparison.Ordinal)) continue;
            if (!code.Contains(".Read(", StringComparison.Ordinal)
                && !code.Contains(".Read()", StringComparison.Ordinal)
                && !code.Contains(".Peek(", StringComparison.Ordinal)
                && !code.Contains(".ReadLine(", StringComparison.Ordinal)
                && !code.Contains(".ReadToEnd(", StringComparison.Ordinal)) continue;
            bool inside = (i >= fill.Start && i <= fill.End) || (i >= line.Start && i <= line.End);
            if (!inside) strays.Add($"SequentialConnector.cs:{i + 1}: {code.Trim()}");
        }
        Assert.True(strays.Count == 0,
            "A character read from the connector's reader outside FillChars/ReadPhysicalLine. Every physical "
            + "read shall pass NextFrame's coherence check, or a sibling connector's release can be hidden "
            + "behind a stale read-ahead (kb/Work PB753). Sites:\n  " + string.Join("\n  ", strays));

        // 3. …and both of those are reached only from the walk.
        var callers = new List<string>();
        foreach (string callee in new[] { "FillChars(", "ReadPhysicalLine(" })
            for (int i = 0; i < lines.Length; i++)
            {
                string code = Strip(lines[i]);
                if (!code.Contains(callee, StringComparison.Ordinal)) continue;
                if (i == fill.Start || i == line.Start) continue;                     // the declarations
                if (i >= frame.Start && i <= frame.End) continue;                     // the one walk
                if (i >= fill.Start && i <= fill.End) continue;                       // FillChars' own loop
                callers.Add($"SequentialConnector.cs:{i + 1}: {code.Trim()}");
            }
        Assert.True(callers.Count == 0,
            "A framing primitive called from outside NextFrame — the walk is where the coherence check lives, "
            + "so a second caller is a second, unchecked read path. Sites:\n  " + string.Join("\n  ", callers));
    }

    /// <summary>⛔ ONE BUFFER BETWEEN THE CONNECTOR AND THE MEDIUM. The connector can discard only the buffer
    /// it owns, so a participant's OS handle shall hold none: <c>HostFile.OpenConnectorStream</c> reads the
    /// buffer size off the §9.1.15 file lock, exactly as <c>OpenConnectorWriteStream</c> reads its repositioning
    /// arm off it. This was proved, not assumed — with the generation in place and this handle still 4096-byte
    /// buffered, a sibling's REWRITE was STILL invisible, because <see cref="FileStream.Seek"/> reuses its read
    /// buffer when the target offset falls inside it.</summary>
    [Fact]
    public void AParticipantsReadHandleCarriesNoBufferOfItsOwn()
    {
        string[] lines = File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "IO", "FileSupport.cs"));
        var body = Body(lines, "public static FileStream OpenConnectorStream(");
        string code = string.Concat(lines[body.Start..(body.End + 1)].Select(Strip));
        Assert.Contains("FileLockPosture.AdmitsAnotherWriter(share)", code, StringComparison.Ordinal);
        Assert.Contains("? 1 :", code, StringComparison.Ordinal);
    }

    /// <summary>The read-coherence rule is a rule about the MEDIUM, so its state reaches EVERY connector —
    /// including the clause-less pair kb/Work PB740 admitted, for which <c>SharedPhysical</c> (the §9.1.16
    /// record-locking view) is null. One writer sets both, so they cannot fall out of step.</summary>
    [Fact]
    public void ThePhysicalStateReachesEveryConnectorAndTheLockingViewIsDerived()
    {
        string[] lines = File.ReadAllLines(TestRepo.Src("Cobol.Net.Runtime", "IO", "FileRegistry.cs"));
        var writes = lines.Select((l, i) => (l, i))
            .Where(p => !p.l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                     && p.l.Contains("AssociatePhysical(", StringComparison.Ordinal))
            .Select(p => $"FileRegistry.cs:{p.i + 1}: {p.l.Trim()}").ToList();
        Assert.True(writes.Count == 1,
            $"The physical-file association must have exactly one writer; found {writes.Count}.\n  "
            + string.Join("\n  ", writes));
        Assert.Contains("_connectorShares.ContainsKey(name)", writes[0], StringComparison.Ordinal);
        // The state itself is UNCONDITIONAL — a clause-less connector on a shared physical file still has to
        // see a sibling's release (kb/Work PB753). If this ever becomes a ternary again the reach is lost.
        Assert.DoesNotContain("? st : null", writes[0], StringComparison.Ordinal);
    }
}
