// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A MERGE ONCE SHIPPED ITS UNRESOLVED CONFLICT HUNKS AND EVERY GATE STAYED GREEN
/// (<c>kb/Work/PB735</c>). Commit <c>c6460d0f</c> staged
/// <c>scripts/spec/audit_grammar_optional_words.py</c> with four conflicted regions still in it — the file
/// stopped parsing (<c>SyntaxError</c> at its first marker) and therefore the audit it implements was dead —
/// and nothing objected, because the wave-local gate is a filtered <c>dotnet test</c> and no test imports that
/// script. A broken tool reads exactly like a silent one.
/// <para>
/// Three layers each had a reason not to look: the implementer checkpoint protocol stages with <c>git add -A</c>,
/// which is as happy to stage a conflicted file as a clean one; the lander briefs resolve conflicts by judgement
/// and had no step that re-checked the resolution; and no test read the file. This class is the layer that makes
/// the other two unnecessary to remember — the marker is a BYTE-LEVEL fact about the committed tree, so it gets a
/// byte-level test over the whole committed tree.
/// </para>
/// <para>
/// ⭐ SCOPED TO TRACKED FILES, AND THAT IS THE DESIGN, NOT AN OPTIMIZATION. The obvious alternative — walk the
/// filesystem and skip a hand-written list of directories — needs that list precisely because
/// <c>tests/external/gnucobol/</c> carries marker-shaped lines as TEST DATA (GnuCOBOL's own <c>syn_misc</c> cases
/// feed them to their compiler on purpose). That corpus is gitignored (<c>.gitignore</c>), so
/// <c>git ls-files</c> never reports it and the exclusion is STRUCTURAL: nothing to maintain, nothing to rot when
/// the corpus moves, and no dead lookup to re-derive later
/// (<c>feedback_a_dead_lookup_is_also_unverified</c>). The enumeration therefore comes from git and only from git,
/// and if git cannot be run the tests go RED rather than quietly sweeping nothing
/// (<c>feedback_verdict_evidence_invariant</c>: a missing observation is not a negative one) — which is also what
/// <see cref="TheSweepActuallyReadsTheTrackedTree"/> asserts.
/// </para>
/// <para>
/// ⭐ THE SCAN IS A FUNCTION, NOT A <c>git grep</c>, so that its FAILURE branch can be fired.
/// <c>feedback_green_gates_arent_evidence</c>: a passing check proves nothing if it never looked at what changed.
/// <see cref="TheSweepFiresOnAPlantedConflict"/> aims <see cref="Sweep"/> — the identical code path the tree sweep
/// uses — at a planted file holding a real conflict hunk and requires all four markers back. Its scratch
/// directory carries a GUID, never a fixed shared path (<c>kb/Work/PB376</c> is the drift test that races itself
/// across agents for want of exactly that).
/// </para>
/// </summary>
public sealed class ConflictMarkerDriftTests
{
    /// <summary>A conflict marker is a run of exactly seven identical characters at the start of a line.</summary>
    private const int Run = 7;

    /// <summary>git's own binary heuristic looks at roughly the first 8 000 bytes for a NUL.</summary>
    private const int ProbeBytes = 8192;

    // ⭐ The marker spellings are BUILT, never written as literals, so THIS FILE is not itself an offender —
    // a guard whose own source trips it is a guard that has to be excluded from its own sweep, and an
    // exclusion is the thing this class exists to avoid.
    private static readonly string Ours = new('<', Run);
    private static readonly string Base = new('|', Run);
    private static readonly string Split = new('=', Run);
    private static readonly string Theirs = new('>', Run);

    /// <summary>Which of the four merge markers a line is.</summary>
    private enum Marker
    {
        None,

        /// <summary>The "ours" opener — seven <c>&lt;</c> then a space or end of line.</summary>
        Ours,

        /// <summary>The diff3/zdiff3 common-ancestor divider — seven <c>|</c>.</summary>
        Base,

        /// <summary>The bare divider — seven <c>=</c> and NOTHING else on the line.</summary>
        Split,

        /// <summary>The "theirs" closer — seven <c>&gt;</c> then a space or end of line.</summary>
        Theirs,
    }

    /// <summary>One marker line, named for the failure message.</summary>
    internal readonly record struct Offender(string Path, int Line, string Text);

    /// <summary>
    /// A tracked file the sweep could not open although it still exists — named with what is needed to attribute
    /// the red without a re-run: the exception, and the file's attributes the moment after the open failed.
    /// </summary>
    internal readonly record struct Unreadable(string Path, string Error, string Attributes);

    /// <summary>What a sweep saw — the offenders AND the population it drew them from.</summary>
    internal sealed record SweepResult(
        IReadOnlyList<Offender> Offenders, int FilesScanned, int FilesBinary, int FilesMissing,
        IReadOnlyList<Unreadable> FilesUnreadable);

    /// <summary>
    /// No file in the committed tree carries an unresolved merge conflict.
    /// </summary>
    [Fact]
    public void NoTrackedFileCarriesAConflictMarker()
    {
        SweepResult r = Sweep(TestRepo.Root, TrackedTree.Files());

        AssertEveryFileWasRead(r);
        Assert.True(r.Offenders.Count == 0,
            $"{r.Offenders.Count} unresolved merge-conflict marker line(s) in the COMMITTED tree "
            + $"({r.FilesScanned} text files scanned):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, r.Offenders.Take(50).Select(o => $"    {o.Path}:{o.Line}  {o.Text}"))
            + Environment.NewLine
            + "Finish the merge. `git diff --check` names them in the working tree and "
            + "`git diff --cached --check` names them in the index — run the staged form between the "
            + "`git add -A` and the `git commit` of every checkpoint, which is where kb/Work/PB735's merge "
            + "would have been stopped.");
    }

    /// <summary>
    /// The sweep found the tree it claims to have swept. Without this, a broken enumeration — no git on PATH, a
    /// wrong working directory, an empty index — makes <see cref="NoTrackedFileCarriesAConflictMarker"/> pass by
    /// looking at nothing, which is the one way a guard like this fails silently
    /// (<c>feedback_verdict_evidence_invariant</c>).
    /// </summary>
    [Fact]
    public void TheSweepActuallyReadsTheTrackedTree()
    {
        IReadOnlyList<string> tracked = TrackedTree.Files();
        Assert.True(tracked.Count >= 1000,
            $"`git ls-files -z` reported only {tracked.Count} tracked path(s) under {TestRepo.Root} — the "
            + "enumeration is broken, so the conflict-marker sweep is scanning nothing.");

        SweepResult r = Sweep(TestRepo.Root, tracked);
        AssertEveryFileWasRead(r);
        Assert.True(r.FilesScanned >= 1000,
            $"the sweep opened {tracked.Count} tracked path(s) but scanned only {r.FilesScanned} as text "
            + $"({r.FilesBinary} classified binary, {r.FilesMissing} absent from the working tree) — the "
            + "binary heuristic or the working tree is wrong.");
    }

    /// <summary>
    /// ⭐ THE WATCHDOG'S FAILURE BRANCH, FIRED. A planted file holding a real four-marker conflict hunk goes
    /// through the SAME <see cref="Sweep"/> the tree test uses, and all four markers must come back with their
    /// line numbers. Until this has failed once on purpose, the tree test's silence means nothing.
    /// <para>
    /// ⚠ RUN OVER BOTH LINE ENDINGS, because the first draft of this fixture held CRLF fixed and passed while
    /// the scanner silently missed every LF-terminated marker — and this repository is LF
    /// (<c>feedback_probe_the_shape_the_subject_hides</c>: flip the axis the subject holds fixed).
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void TheSweepFiresOnAPlantedConflict(string eol)
    {
        using var scratch = new Scratch();
        // A verbatim diff3-style hunk, assembled from the built spellings.
        scratch.Write("conflicted.txt", string.Join(eol,
        [
            "context above",          // 1
            Ours + " HEAD",           // 2
            "our side",               // 3
            Base + " merged common",  // 4
            "the ancestor",           // 5
            Split,                    // 6
            "their side",             // 7
            Theirs + " origin/main",  // 8
            "context below",          // 9
        ]) + eol);

        SweepResult r = Sweep(scratch.Root, ["conflicted.txt"]);

        Assert.Equal(1, r.FilesScanned);
        // The offender carries the MARKER, not the branch label the merge wrote after it — the label is noise
        // in a failure message and differs per merge.
        Assert.Equal(
            new[] { (2, Ours), (4, Base), (6, Split), (8, Theirs) },
            r.Offenders.Select(o => (o.Line, o.Text)).ToArray());
    }

    /// <summary>
    /// ⭐ THE COMPANY RULE'S NEGATIVE ARM, in both its forms. A bare seven-character <c>=</c> line is a legal
    /// Markdown setext H1 underline and a bare seven-character <c>|</c> line is a legal empty Markdown table row,
    /// so a divider is an offender only in the company of an opener or a closer in the same file — otherwise this
    /// guard would red-flag ordinary prose, and the repair for that would be the per-path exclusion list the whole
    /// design avoids.
    /// </summary>
    [Fact]
    public void ADividerWithoutItsBracketsIsNotAConflict()
    {
        using var scratch = new Scratch();
        scratch.Write("heading.md", "Title\r\n" + Split + "\r\n\r\nbody text\r\n");
        scratch.Write("table.md", "| a | b | c | d | e | f |\r\n| - | - | - | - | - | - |\r\n" + Base + "\r\n");

        SweepResult r = Sweep(scratch.Root, ["heading.md", "table.md"]);

        Assert.Equal(2, r.FilesScanned);
        Assert.Empty(r.Offenders);
    }

    /// <summary>
    /// A binary file is not read as lines. The heuristic is a NUL in the first <see cref="ProbeBytes"/> bytes —
    /// git's own — so a new binary format needs no registration anywhere.
    /// </summary>
    [Fact]
    public void ABinaryFileIsNotScanned()
    {
        using var scratch = new Scratch();
        byte[] blob = [0x00, 0x01, 0x02, .. Encoding.ASCII.GetBytes("binary\r\n" + Ours + " HEAD\r\n")];
        scratch.WriteBytes("blob.bin", blob);

        SweepResult r = Sweep(scratch.Root, ["blob.bin"]);

        Assert.Equal(0, r.FilesScanned);
        Assert.Equal(1, r.FilesBinary);
        Assert.Empty(r.Offenders);
    }

    /// <summary>
    /// ⛔ kb/Work/PB1583 — THE SWEEP IS A READER AMONG WRITERS. It runs beside the Conformance leg, a lander's
    /// <c>git</c>, an editor and every other gate on the host, over a working tree any of them may be writing.
    /// A read-only scan must therefore never be the handle that is refused: it opens with
    /// <see cref="FileShare.ReadWrite"/> | <see cref="FileShare.Delete"/>, so a file another handle holds open
    /// for writing (or for a rename) is still read. The retired <see cref="File.OpenRead(string)"/> asked for
    /// <see cref="FileShare.Read"/>, which Windows refuses outright while any writer's handle is outstanding —
    /// the open this test holds.
    /// </summary>
    [Fact]
    public void TheSweepReadsAFileAnotherHandleIsWriting()
    {
        using var scratch = new Scratch();
        scratch.Write("in-flight.txt", Ours + " HEAD\n" + Theirs + " origin/main\n");

        using (new FileStream(Path.Combine(scratch.Root, "in-flight.txt"), FileMode.Open, FileAccess.Write,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            SweepResult r = Sweep(scratch.Root, ["in-flight.txt"]);

            Assert.True(r.FilesUnreadable.Count == 0, Describe(r.FilesUnreadable));
            Assert.Equal(1, r.FilesScanned);
            Assert.Equal(new[] { 1, 2 }, r.Offenders.Select(o => o.Line).ToArray());
        }
    }

    /// <summary>
    /// ⭐ THE UNREADABLE ARM, FIRED. A file the sweep cannot open is REPORTED — path, exception, attributes —
    /// never skipped, and never thrown past the files after it: golden lane #2's red (DEVLOG 1700) died on one
    /// <c>.err</c> with no sweep result at all, so whether one file or a directory of them was refused was
    /// unknowable. An exclusive handle is the plant; on a host whose share modes cannot refuse a reader
    /// (<see cref="HostCapability.Sharing"/>, measured) the same file must instead be read.
    /// </summary>
    [Fact]
    public void AFileTheSweepCannotOpenIsReportedNotSkipped()
    {
        using var scratch = new Scratch();
        scratch.Write("locked.txt", "plain text\n");
        scratch.Write("after.txt", Ours + " HEAD\n");

        using (new FileStream(Path.Combine(scratch.Root, "locked.txt"), FileMode.Open, FileAccess.ReadWrite,
                   FileShare.None))
        {
            SweepResult r = Sweep(scratch.Root, ["locked.txt", "after.txt"]);

            // The file AFTER the unreadable one is still swept.
            Assert.Equal(new[] { "after.txt" }, r.Offenders.Select(o => o.Path).ToArray());
            if (HostCapability.Sharing.ExclusiveRefusesAnOutsideReader)
            {
                Unreadable u = Assert.Single(r.FilesUnreadable);
                Assert.Equal("locked.txt", u.Path);
                Assert.StartsWith(nameof(IOException), u.Error, StringComparison.Ordinal);
                Assert.Equal(1, r.FilesScanned);
            }
            else
            {
                Assert.True(r.FilesUnreadable.Count == 0,
                    $"{HostCapability.Sharing.Because}\n{Describe(r.FilesUnreadable)}");
                Assert.Equal(2, r.FilesScanned);
            }
        }
    }

    /// <summary>
    /// A sweep that could not read a file cannot say the file is clean: RED, naming each file, its exception
    /// and its attributes, so the red is attributable from this message alone (kb/Work/PB1573's rule, applied
    /// to the evidence this test produces).
    /// </summary>
    private static void AssertEveryFileWasRead(SweepResult r) =>
        Assert.True(r.FilesUnreadable.Count == 0,
            $"the conflict-marker sweep could not open {r.FilesUnreadable.Count} tracked file(s) that still "
            + "exist, so it cannot say they carry no marker:" + Environment.NewLine + Describe(r.FilesUnreadable));

    private static string Describe(IReadOnlyList<Unreadable> unreadable) =>
        string.Join(Environment.NewLine,
            unreadable.Take(50).Select(u => $"    {u.Path}: {u.Error} (attributes: {u.Attributes})"));

    /// <summary>
    /// Scans <paramref name="relativePaths"/> under <paramref name="root"/> for unresolved merge markers.
    /// </summary>
    /// <remarks>
    /// Byte-level and streaming on purpose: the tracked tree is ~140 MB and the largest text member
    /// (<c>tests/nist/extracted/newcob.val</c>) is 28 MB, so decoding every file to strings and allocating one
    /// string per line would put megabytes of garbage and a full Unicode decode on a gate that runs beside 5 000
    /// other tests. The markers are ASCII, so the bytes answer the question directly; the buffer is reused across
    /// files and the per-line state is four locals.
    /// </remarks>
    internal static SweepResult Sweep(string root, IEnumerable<string> relativePaths)
    {
        var hits = new List<Offender>();
        var unreadable = new List<Unreadable>();
        int scanned = 0, binary = 0, missing = 0;
        byte[] buf = new byte[64 * 1024];
        var perFile = new List<(int Line, Marker Kind)>();

        foreach (string rel in relativePaths)
        {
            string full = Path.Combine(root, rel);
            if (!File.Exists(full)) { missing++; continue; }

            perFile.Clear();
            // ⛔ kb/Work/PB1583: share EVERYTHING — a read-only scan must never be the handle that refuses, and
            // must never be refused because another process is writing or renaming the file. bufferSize 1
            // turns FileStream's own buffer off; `buf` is the only one.
            FileStream s;
            try
            {
                s = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1, FileOptions.SequentialScan);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A file that VANISHED between the listing and the open is absent from the working tree — the
                // `missing` population above, by the same rule. Anything still there is a file this sweep did
                // not read, and is reported as such.
                if (!File.Exists(full)) { missing++; continue; }
                unreadable.Add(new Unreadable(rel.Replace('\\', '/'), $"{e.GetType().Name}: {e.Message}",
                    File.GetAttributes(full).ToString()));
                continue;
            }

            using (s)
            {
                if (!ScanStream(s, buf, perFile)) { binary++; continue; }
            }

            scanned++;
            if (perFile.Count == 0) continue;

            // ⭐ THE COMPANY RULE. An opener or a closer is self-evidencing: seven `<` or `>` at column 0 has no
            // other use in this repo. The two DIVIDERS do — `=======` is a legal Markdown setext H1 underline and
            // `|||||||` is a legal (if odd) empty Markdown table row — and git never writes either without its
            // brackets, so a divider counts only in the company of an opener or closer IN THE SAME FILE. That
            // keeps the guard's false-positive rate at zero without a per-path exclusion list.
            bool bracketed = perFile.Exists(h => h.Kind is Marker.Ours or Marker.Theirs);
            foreach ((int line, Marker kind) in perFile)
            {
                if (kind is Marker.Split or Marker.Base && !bracketed) continue;
                hits.Add(new Offender(rel.Replace('\\', '/'), line, Spelling(kind)));
            }
        }

        return new SweepResult(hits, scanned, binary, missing, unreadable);
    }

    /// <summary>
    /// Reads <paramref name="s"/> once, appending every marker line to <paramref name="hits"/>.
    /// Returns <c>false</c> when the stream is binary (a NUL inside the first <see cref="ProbeBytes"/> bytes).
    /// </summary>
    private static bool ScanStream(Stream s, byte[] buf, List<(int Line, Marker Kind)> hits)
    {
        int line = 1;
        int col = 0;        // how far into the candidate run this line has got
        byte mc = 0;        // the character the run is made of, 0 before the first byte of a line
        bool dead = false;  // this line is settled — it is not a marker, or it already was one

        int n = Fill(s, buf);
        if (buf.AsSpan(0, Math.Min(n, ProbeBytes)).IndexOf((byte)0) >= 0) return false;

        while (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                byte b = buf[i];
                if (b == (byte)'\n')
                {
                    // ⛔ THE LINE FEED IS ALSO AN END OF LINE, and this arm is the one that gets forgotten.
                    // A CRLF file settles a marker on its CR in the branch below; an LF-ONLY file reaches the
                    // newline with the run still pending, so without this the whole repo — which is LF — would
                    // have gone unchecked for every marker git writes without a label after it, `=======` first
                    // among them. Two arms, one rule (feedback_two_arm_dispatch).
                    if (!dead && col == Run && mc != 0) hits.Add((line, KindOf(mc)));
                    line++; col = 0; mc = 0; dead = false;
                    continue;
                }

                if (dead) continue;

                if (col == 0)
                {
                    mc = b is (byte)'<' or (byte)'|' or (byte)'=' or (byte)'>' ? b : (byte)0;
                    dead = mc == 0;
                    col = 1;
                }
                else if (col < Run)
                {
                    if (b != mc) dead = true;
                    col++;
                }
                else
                {
                    // The eighth byte decides: an opener/closer takes a space or the end of the line, a bare
                    // divider takes only the end of the line. Either way the line is settled here.
                    bool eol = b == (byte)'\r';
                    if (mc == (byte)'=' ? eol : eol || b == (byte)' ') hits.Add((line, KindOf(mc)));
                    dead = true;
                }
            }

            n = Fill(s, buf);
        }

        // End of file terminates the last line just as a newline would.
        if (!dead && col == Run && mc != 0) hits.Add((line, KindOf(mc)));
        return true;
    }

    /// <summary>Reads until <paramref name="buf"/> is full or the stream ends; returns the byte count.</summary>
    private static int Fill(Stream s, byte[] buf)
    {
        int n = 0;
        while (n < buf.Length)
        {
            int k = s.Read(buf, n, buf.Length - n);
            if (k == 0) break;
            n += k;
        }

        return n;
    }

    private static Marker KindOf(byte c) => c switch
    {
        (byte)'<' => Marker.Ours,
        (byte)'|' => Marker.Base,
        (byte)'=' => Marker.Split,
        _ => Marker.Theirs,
    };

    private static string Spelling(Marker m) => m switch
    {
        Marker.Ours => Ours,
        Marker.Base => Base,
        Marker.Split => Split,
        _ => Theirs,
    };

    /// <summary>A GUID-unique scratch directory — never a fixed shared path (<c>kb/Work/PB376</c>).</summary>
    private sealed class Scratch : IDisposable
    {
        public Scratch() => Directory.CreateDirectory(Root);

        public string Root { get; } =
            Path.Combine(Path.GetTempPath(), "cobolnet-conflict-marker-" + Guid.NewGuid().ToString("N"));

        public void Write(string name, string text) => WriteBytes(name, Encoding.UTF8.GetBytes(text));

        public void WriteBytes(string name, byte[] bytes) => File.WriteAllBytes(Path.Combine(Root, name), bytes);

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) { /* a scratch directory left behind is not a test failure */ }
        }
    }
}
