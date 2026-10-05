// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD ON <see cref="FileControlKeyRules"/>'S TABLE (kb/Work PB699).
/// <para>The table is the compiler's whole statement of what the FILE CONTROL ENTRY's key clauses require
/// (ISO/IEC 1989:2023 §12.4.5.1 · §12.4.5.2 SR10 · §12.4.5.6.3 · §12.4.5.12.3 · §12.4.5.13.3). CLAUDE.md rule 1
/// names the failure mode it is exposed to: a clause number is INHERITED rather than re-derived, and
/// <c>cite.py --check</c> on the number alone passes because the quoted sentence really is somewhere in the
/// standard. This guard re-derives EVERY row from <c>specs/ISO_COBOL.md</c> on every run — the clause region, and
/// where the row names a printed ordinal, that ordinal's own sentence — so the row cannot carry a number the
/// standard does not agree with, and a transcription repair flows through instead of going stale.</para>
/// <para>It also closes the loop rule 5 asks for: adding the next key syntax rule must be adding a ROW, not
/// writing a new <c>if</c> somewhere else. <see cref="EveryScreenedRuleInTheInventory_IsARowInTheTable"/>
/// asserts the traceability inventory and the table name the same set of rules, so a rule screened here without a
/// row (or a row claimed there without an implementation) is red.</para>
/// </summary>
public sealed class FileControlKeyRuleDriftTests
{
    // ⛔ THE SPEC READER IS SHARED, NOT COPIED (kb/Work PB721). These three operations — normalize, take a
    // clause's own region, index its printed ordinals — are what EVERY rule-table drift guard in this repository
    // needs, and the second such table (RecordClauseRuleDriftTests) was the moment to extract rather than to
    // paste. The implementation is tests/_shared/SpecClauseText.cs; these forwarders keep the assertions below
    // reading as they did. The local names stay because they are this file's vocabulary, not a second mechanism.
    private static string Norm(string s) => SpecClauseText.Norm(s);

    private static string[] ClauseRegion(string[] lines, string clause) =>
        SpecClauseText.ClauseRegion(lines, clause);

    private static Dictionary<int, string> NumberedRules(string[] region) =>
        SpecClauseText.NumberedRules(region);

    private static string[] SpecLines() => SpecClauseText.Lines();

    // ── The guard itself ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE SCANNER IS PROVEN TO BE ABLE TO FAIL BEFORE ANY ROW IS TRUSTED (a green check that never
    /// looked at anything is not evidence). §12.4.5.13.3 has exactly three printed syntax rules; the region is
    /// bounded by the NEXT heading, so a sentence from the neighbouring clause must NOT be found in it.</summary>
    [Fact]
    public void TheClauseScanner_FindsTheRulesAndRejectsAForeignSentence()
    {
        var lines = SpecLines();
        var rules = NumberedRules(ClauseRegion(lines, "12.4.5.13.3"));
        Assert.Equal(3, rules.Count);
        Assert.Contains("shall not be subject to any OCCURS clauses", rules[1]);

        // §12.4.5.12.3 SR1's sentence names data-name-2 as well; §12.4.5.13.3 SR1 does not. A region that
        // silently swallowed its neighbours would find it here.
        string foreign = Norm("Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses");
        Assert.DoesNotContain(ClauseRegion(lines, "12.4.5.13.3"), l => Norm(l).Contains(foreign));
        // …and it IS in the clause that really states it, so the check is not vacuous.
        Assert.Contains(ClauseRegion(lines, "12.4.5.12.3"), l => Norm(l).Contains(foreign));
    }

    /// <summary>Every row's RULE TEXT is inside the clause the row cites — the mechanical
    /// <c>cite.py --check</c> guarantee, applied to the table itself rather than to a comment about it.</summary>
    [Fact]
    public void EveryRow_QuotesTextThatIsInsideTheClauseItCites()
    {
        var lines = SpecLines();
        Assert.True(FileControlKeyRules.Catalog.Count >= 16,
            $"only {FileControlKeyRules.Catalog.Count} rows — the table lost rules; fix the table, do not lower the floor.");
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            string needle = Norm(rule.RuleText);
            Assert.True(ClauseRegion(lines, rule.Clause).Any(l => Norm(l).Contains(needle)),
                $"§{rule.Clause} does not contain \"{rule.RuleText}\" — the row's clause number is wrong or the text drifted.");
        }
    }

    /// <summary>⛔ THE DIAGRAM HALF of the §12.4.5.1 Format 1 row, which no sentence can carry: the requirement
    /// is that the printed indexed format writes the RECORD KEY clause with NO bracket around it. Asserted on the
    /// RAW transcription line, because the general row check normalizes punctuation away and the diagram is
    /// nothing but punctuation. The FILE STATUS and ALTERNATE RECORD KEY clauses of the same figure are the
    /// contrast: both ARE bracketed, so a figure that had lost its brackets altogether would fail here too.
    /// <para>⚠ If this ever fails, RENDER THE PDF PAGE (<c>scripts/render-spec-page.py</c>) before changing the
    /// rule — the OCR'd diagrams were systematically lossy toward falsely-restrictive syntax (CLAUDE.md rule 1).</para></summary>
    [Fact]
    public void IndexedFormat_PrintsTheRecordKeyClauseUnbracketed()
    {
        var region = ClauseRegion(SpecLines(), "12.4.5.1");
        // The PRIME key clause is the one whose line BEGINS with the clause (nothing to its left but blanks) —
        // the ALTERNATE clause's line begins with the '│' of its own optional bracket, which is the contrast.
        Assert.Single(region, l => l.TrimStart().StartsWith("<u>RECORD</u> KEY IS", StringComparison.Ordinal));

        // The contrast, in the same figure: an OPTIONAL clause is written inside a bracket.
        Assert.Contains(region, l => l.TrimStart().StartsWith("[ FILE <u>STATUS</u> IS", StringComparison.Ordinal));
        Assert.Contains(region, l => l.Contains("<u>ALTERNATE</u> <u>RECORD</u> KEY IS", StringComparison.Ordinal)
                                     && l.TrimStart().StartsWith('│'));
    }

    /// <summary>Where a row names a printed ORDINAL (its inventory rule-id carries one), the text it quotes is
    /// that rule's own sentence — not merely somewhere in the clause. This is the half a clause-only check misses:
    /// "a real clause can answer a different question".</summary>
    [Fact]
    public void EveryRow_QuotesTheOrdinalItNames()
    {
        var lines = SpecLines();
        int checkedRows = 0;
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            if (rule.RuleId is null) continue;
            var m = Regex.Match(rule.RuleId, @"^SR-(?<clause>[0-9.]+)-(?<n>\d+)$");
            Assert.True(m.Success, $"rule-id '{rule.RuleId}' is not the inventory's SR-<clause>-<n> shape.");
            Assert.Equal(rule.Clause, m.Groups["clause"].Value);
            var numbered = NumberedRules(ClauseRegion(lines, rule.Clause));
            int n = int.Parse(m.Groups["n"].Value);
            Assert.True(numbered.ContainsKey(n), $"§{rule.Clause} has no printed syntax rule {n}.");
            Assert.Contains(Norm(rule.RuleText), Norm(numbered[n]));
            checkedRows++;
        }
        Assert.True(checkedRows >= 15, $"only {checkedRows} rows carry an ordinal — the guard is measuring almost nothing.");
    }

    /// <summary>⛔ THE "READ THE WHOLE PRINTED RULE" CLAMP (kb/Work PB742 / PB743). Three of this table's rules
    /// state TWO obligations in one numbered sentence, and in each case the compiler once screened one arm and
    /// shipped the other: §12.4.5.2 SR8 and SR9 each add "The associated file description entry shall not be a
    /// sort-merge file description entry" to their format sentence, and §12.4.5.12.3 SR2 / §12.4.5.6.3 SR2 each
    /// join a CATEGORY to a LOCATION with the word "within". This test re-derives the split FROM THE SPEC — it
    /// finds the sentence boundary in the printed rule itself — and asserts the table carries a row quoting each
    /// side. A rule that grows a third obligation makes it fail rather than silently shipping two thirds.
    /// <para>§12.4.5.2 SR11 (kb/Work PB773) is the fourth member. SR13 prints the same two-sentence shape and is
    /// deliberately NOT in this list: its first sentence ("Format 4 shall be specified only for a sort-merge
    /// file") cannot be broken by any entry, because Format 4 is a SUBSET of Format 3 — an entry with only SELECT,
    /// ASSIGN and SEQUENTIAL is a legal Format 3 entry for an FD — so the one row SR13 has (the SD's entry shall
    /// be Format 4) quotes its second sentence alone. <see cref="EveryEntryClause_IsCarriedByExactlyTheFormatsThatPrintIt"/>
    /// pins the subset relation, which is what makes that statement checkable rather than asserted.</para></summary>
    [Theory]
    [InlineData("12.4.5.2", 8)]
    [InlineData("12.4.5.2", 9)]
    [InlineData("12.4.5.2", 11)]
    [InlineData("12.4.5.12.3", 2)]
    [InlineData("12.4.5.6.3", 2)]
    public void ARuleWithTwoObligations_HasARowForEachArm(string clause, int ordinal)
    {
        string printed = NumberedRules(ClauseRegion(SpecLines(), clause))[ordinal];
        // The two arms as the STANDARD prints them: SR8/SR9 use a sentence break, both SR2s the word "within".
        var arms = printed.Contains(". The associated file description entry", StringComparison.Ordinal)
            ? printed.Split(". The associated", 2, StringSplitOptions.None) is [var head, var tail]
                ? new[] { head, "The associated" + tail }
                : throw new InvalidOperationException("unreachable")
            : printed.Split(" within ", 2, StringSplitOptions.None) is [var cat, var loc]
                ? new[] { cat, "within " + loc }
                : throw new InvalidOperationException(
                    $"§{clause} rule {ordinal} no longer reads as two arms — re-derive the split before trusting the rows.");
        Assert.Equal(2, arms.Length);

        string id = $"SR-{clause}-{ordinal}";
        var rows = FileControlKeyRules.Catalog.Where(r => r.RuleId == id).ToList();
        Assert.True(rows.Count >= 2, $"{id} states two obligations but only {rows.Count} row(s) quote it.");
        foreach (string arm in arms)
            Assert.True(rows.Any(r => Norm(arm).Contains(Norm(r.RuleText))),
                $"no row of {id} quotes the arm \"{arm.Trim()}\" — one obligation of the rule has no screen.");
    }

    /// <summary>A row screened on a SORT-MERGE file quotes the sentence that speaks about one, and cites the only
    /// clause that states it. §12.4.5.2 SR8/SR9/SR11/SR13 are the whole of that set: no key clause's own rules
    /// mention a sort-merge file description entry, and a row that reached an SD without one would be a rule fired
    /// on a file it was never written about.</summary>
    [Fact]
    public void EverySortMergeRow_QuotesTheSortMergeSentenceOfSection12452()
    {
        var rows = FileControlKeyRules.Catalog.Where(r => r.ScreenedOn.HasFlag(FileKinds.SortMerge)).ToList();
        Assert.NotEmpty(rows);
        foreach (var r in rows)
        {
            Assert.Equal("12.4.5.2", r.Clause);
            Assert.Contains("sort merge file description entry", Norm(r.RuleText));
            Assert.Equal(FileKeyRole.Entry, r.Role);
        }
        // …and the complement: no OTHER row is screened on a sort-merge entry, which is what makes the deleted
        // `if (file.IsSortMerge) return;` unnecessary rather than merely absent.
        Assert.All(FileControlKeyRules.Catalog.Where(r => r.Role != FileKeyRole.Entry),
            r => Assert.False(r.ScreenedOn.HasFlag(FileKinds.SortMerge)));
    }

    /// <summary>What each <see cref="FileControlKeyRules.EntryClauseCatalog"/> row looks like IN A PRINTED DIAGRAM, with
    /// the diagram's markup stripped. This is the ORACLE the table is checked against, written independently of
    /// the table: a row that carries a clause in a format whose diagram does not print it — or omits one it does —
    /// is red, so the set of formats per clause cannot drift from §12.4.5.1.</summary>
    private static readonly (string Face, Regex Printed)[] DiagramOracle =
    [
        ("ORGANIZATION IS INDEXED", new(@"\bINDEXED\b")),
        ("a RECORD KEY clause", new(@"(?<!ALTERNATE )RECORD KEY IS")),
        ("an ALTERNATE RECORD KEY clause", new(@"ALTERNATE RECORD KEY IS")),
        ("a COLLATING SEQUENCE clause", new(@"collating-sequence-clause")),
        ("ORGANIZATION IS RELATIVE", new(@"ORGANIZATION IS \] +RELATIVE")),
        ("a RELATIVE KEY clause", new(@"RELATIVE KEY IS")),
        ("ORGANIZATION IS LINE SEQUENTIAL", new(@"\bLINE\b")),
        ("a RECORD DELIMITER clause", new(@"RECORD DELIMITER IS")),
        // `[ ORGANIZATION IS ] … SEQUENTIAL` on ONE line: Format 4 prints it bare, Format 3 inside the LINE/RECORD brace.
        ("ORGANIZATION IS SEQUENTIAL", new(@"ORGANIZATION IS \][^\n]*SEQUENTIAL")),
        ("ACCESS MODE IS SEQUENTIAL", new(@"ACCESS MODE IS")),          // every format that prints ACCESS prints SEQUENTIAL
        ("ACCESS MODE IS RANDOM or DYNAMIC", new(@"\bRANDOM\b")),
        ("a FILE STATUS clause", new(@"FILE STATUS IS")),
        ("a LOCK MODE clause", new(@"LOCK MODE IS")),
        ("a LOCK MODE clause with WITH LOCK ON MULTIPLE RECORDS", new(@"\bMULTIPLE\b")),
        ("a RESERVE clause", new(@"RESERVE integer-1")),
        ("a SHARING clause", new(@"SHARING WITH")),
    ];

    /// <summary>The four general formats' diagrams as printed text, keyed by format number, markup stripped.</summary>
    private static Dictionary<FileFormats, string> PrintedFormats()
    {
        var region = ClauseRegion(SpecLines(), "12.4.5.1");
        var blocks = new Dictionary<FileFormats, string>();
        FileFormats? current = null;
        bool inPre = false;
        var text = new System.Text.StringBuilder();
        void Flush()
        {
            if (current is { } c) blocks[c] = Regex.Replace(text.ToString(), "</?u>", "");
            text.Clear();
        }
        foreach (var line in region)
        {
            var m = Regex.Match(line, @"^Format (?<n>[1-4]) \(");
            if (m.Success)
            {
                Flush();
                current = (FileFormats)(1 << (int.Parse(m.Groups["n"].Value) - 1));
                inPre = false;
            }
            else if (line.StartsWith("<pre", StringComparison.Ordinal)) inPre = true;
            else if (line.StartsWith("</pre>", StringComparison.Ordinal)) inPre = false;
            else if (inPre) text.Append(line).Append('\n');
        }
        Flush();
        return blocks;
    }

    /// <summary>⛔ THE FORMAT-MEMBERSHIP TABLE IS RE-DERIVED FROM THE FOUR PRINTED DIAGRAMS ON EVERY RUN (kb/Work
    /// PB773). <see cref="FileControlKeyRules.EntryClauseCatalog"/> says which §12.4.5.1 formats print each clause
    /// that is not in all four, and SR8, SR9, SR11 and SR13 are decided by it; this test reads the diagrams and
    /// asserts, per clause and per format, that the table agrees. It also states the SUBSET relation the SR13
    /// design rests on — Format 4 prints nothing that Format 3 does not — and that SELECT/ASSIGN, the clauses in no
    /// row, really are in all four.</summary>
    [Fact]
    public void EveryEntryClause_IsCarriedByExactlyTheFormatsThatPrintIt()
    {
        var printed = PrintedFormats();
        Assert.Equal(4, printed.Count);
        Assert.All(printed.Values, b => Assert.Contains("SELECT", b));
        Assert.All(printed.Values, b => Assert.Contains("ASSIGN", b));   // the clause in every format is in no row

        var catalog = FileControlKeyRules.EntryClauseCatalog;
        Assert.Equal(DiagramOracle.Select(o => o.Face).Order(), catalog.Select(c => c.Face).Order());
        foreach (var (face, regex) in DiagramOracle)
        {
            var row = catalog.Single(c => c.Face == face);
            foreach (var (format, block) in printed)
                Assert.True(row.Carried.HasFlag(format) == regex.IsMatch(block),
                    $"'{face}': the table says Format {(int)Math.Log2((int)format) + 1} "
                    + $"{(row.Carried.HasFlag(format) ? "prints" : "does not print")} it, the printed diagram "
                    + $"{(regex.IsMatch(block) ? "does" : "does not")} — fix the row, or re-render the page.");
        }
        // The subset fact: no clause is carried by Format 4 without Format 3.
        Assert.All(catalog.Where(c => c.Carried.HasFlag(FileFormats.Format4)),
            c => Assert.True(c.Carried.HasFlag(FileFormats.Format3), c.Face));
    }

    /// <summary>The message a row ships names the row's OWN citation. A row whose sentence and whose printed §
    /// disagree is exactly the defect this file exists for, and the message is what a user reads.</summary>
    [Fact]
    public void EveryRow_ShipsAMessageThatNamesItsOwnCitation()
    {
        var file = new FileModel { CobolName = "F", SelectName = "F", AssignTarget = "F" };
        // ⛔ THE OPERAND CARRIES A REAL ITEM. It used to be null, so every row that RENDERS the operand's
        // description — the category arms of both SR2s name what the key IS — was rendered down its absent-item
        // branch and the test proved nothing about the message a program actually reads (kb/Work PB743).
        var item = new DataItem { Level = 5, CobolName = "K", CsName = "K" };
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            // An ALTERNATE RECORD KEY operand carries a SUPPRESS WHEN literal-1 too, so the §12.4.5.6.3 SR7 rows
            // render the operand they actually describe (kb/Work PB1072).
            var op = new FileKeyOperand(rule.Role, "KEY", "K", item, default,
                SuppressWhen: new SuppressWhenOperand("ALL 'AB'", SuppressWhenForm.AllLiteral,
                    CobolNet.Common.LiteralClass.Alphanumeric, "AB", null));
            string message = rule.Message(file, op);
            Assert.Contains(rule.Citation, message);
        }
    }

    /// <summary>⛔ THE INVARIANT THAT MAKES <see cref="FileControlKeyRule.ScreenedOn"/> LOAD-BEARING, and the one
    /// a scalar column could not state. A row is screened either on EXACTLY the kind whose §12.4.5.1 format
    /// carries its clause — a rule stated INSIDE the format — or on a set DISJOINT from it, which is what
    /// §12.4.5.2 SR8/SR9 are: rules about a clause written where its format does not apply. A PARTIAL overlap
    /// would be a rule the standard does not state, and only §12.4.5.2 can hold the disjoint half, because it is
    /// the only clause whose syntax rules are about which format an entry may be written in.</summary>
    [Fact]
    public void EveryRow_IsScreenedEitherOnItsOwnFormatOrDisjointFromIt()
    {
        foreach (var rule in FileControlKeyRules.Catalog)
        {
            if (rule.Role is FileKeyRole.Entry)
            {
                // An entry rule speaks either about an SD (the "shall not be a sort-merge file description
                // entry" sentence SR8, SR9 and SR11 close with, and SR13) or — SR11's FIRST sentence — about the
                // files its format is not for. Format 3's own kinds are the sequential ones, and the screened set
                // must stay clear of them: a row over a sequential file would be a rule about the format it allows.
                if (rule.ScreenedOn == FileKinds.SortMerge) continue;
                Assert.Equal("12.4.5.2", rule.Clause);
                Assert.NotEqual(FileKinds.None, rule.ScreenedOn);
                Assert.Equal(FileKinds.None, rule.ScreenedOn & (FileKinds.SortMerge | FileKinds.Sequential | FileKinds.LineSequential));
                continue;
            }
            var own = rule.Role is FileKeyRole.RelativeKey ? FileKinds.Relative : FileKinds.Indexed;
            if (rule.ScreenedOn == own) continue;
            Assert.Equal(FileKinds.None, rule.ScreenedOn & own);
            Assert.Equal("12.4.5.2", rule.Clause);
        }
    }

    /// <summary>⛔ PROVE THE SET ARITHMETIC CAN FAIL before the rows are trusted: <see cref="FileKinds"/>'s
    /// members shall be the one-hot bits <c>FileControlKeyRules.KindOf</c> computes from a
    /// <see cref="FileOrganization"/>, or a row's set would silently miss the kind it names. Every organization
    /// is checked, and the complement sets the SR8/SR9 rows use are checked to EXCLUDE their own format — the
    /// half a "does it contain the right ones" assertion would pass without.</summary>
    [Fact]
    public void TheKindSet_IsOneHotOverEveryOrganizationAndTheComplementsExcludeTheirOwn()
    {
        foreach (FileOrganization org in Enum.GetValues<FileOrganization>())
        {
            var kind = (FileKinds)(1 << (int)org);
            Assert.True(Enum.IsDefined(kind), $"FileKinds has no member for ORGANIZATION {org}.");
            Assert.True(FileKinds.AnyOrganization.HasFlag(kind));
            Assert.False(FileKinds.SortMerge.HasFlag(kind));
        }
        Assert.False((FileKinds.AnyOrganization & ~FileKinds.Indexed).HasFlag(FileKinds.Indexed));
        Assert.True((FileKinds.AnyOrganization & ~FileKinds.Indexed).HasFlag(FileKinds.Sequential));
        Assert.True((FileKinds.AnyOrganization & ~FileKinds.Indexed).HasFlag(FileKinds.LineSequential));
        Assert.False((FileKinds.AnyOrganization & ~FileKinds.Relative).HasFlag(FileKinds.Relative));
        Assert.True((FileKinds.AnyOrganization & ~FileKinds.Relative).HasFlag(FileKinds.Indexed));
    }

    /// <summary>⭐ THE "NEXT RULE IS A ROW" CLAMP, in both directions.
    /// <list type="bullet">
    /// <item>A rule the inventory says is screened HERE must have a row — otherwise it is enforced somewhere else,
    /// which is the shape PB699 was.</item>
    /// <item>A row's rule-id must be a REAL inventory rule-id, and if that row has already been credited to a
    /// code-location, that location must be this file — a row claiming an id the inventory files against a
    /// different site is a claim on the burn-down the code does not back.</item>
    /// </list>
    /// A table row whose inventory row carries NO code-location yet is legal and deliberate: a rule may be
    /// screened before it has earned a verdict (the two SR2s are screened only in their within-a-record arm, so
    /// neither CONFORMS nor a non-resolving promise would be true of them — see the class's "NOT HERE"
    /// paragraph). Demanding a verdict here would push the register into recording one it has not earned.</summary>
    [Fact]
    public void EveryScreenedRuleInTheInventory_IsARowInTheTable()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            TestRepo.At("tests", "version-matrix", "traceability-inventory.json")));
        var claimed = new SortedSet<string>(StringComparer.Ordinal);       // credited to THIS file
        var locatedElsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        var allIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in doc.RootElement.EnumerateArray())              // a top-level array of rows
        {
            string id = row.GetProperty("rule-id").GetString()!;
            allIds.Add(id);
            string where = row.TryGetProperty("code-location", out var cl) ? cl.GetString() ?? "" : "";
            if (where.Contains("FileControlKeyRules", StringComparison.Ordinal)) claimed.Add(id);
            else if (where.Length > 0) locatedElsewhere[id] = where;
        }
        var inTable = new SortedSet<string>(
            FileControlKeyRules.Catalog.Where(r => r.RuleId is not null).Select(r => r.RuleId!), StringComparer.Ordinal);

        Assert.True(claimed.Count > 0,
            "no inventory row points at FileControlKeyRules — either the batch was never applied or the screen moved.");
        Assert.Empty(claimed.Except(inTable));                             // screened here per the register, no row
        foreach (string id in inTable)
        {
            Assert.True(allIds.Contains(id), $"row '{id}' is not a rule-id the traceability inventory knows.");
            Assert.False(locatedElsewhere.TryGetValue(id, out string? where),
                $"row '{id}' is screened here, but the inventory credits it to '{where}'.");
        }
    }
}
