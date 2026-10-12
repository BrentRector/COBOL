## Entry 1947 — 2026-10-09 10:14 PDT — Train 1049: the last 2 % of both accounts — PB2757 (sequential medium bytes never BOM-sniffed), PB2739 (one DivisionCursor rule), PB2844 (a constant's LENGTH OF through TYPE / SAME AS)

Owner, 2026-10-09 ~09:55: "Both your and Mythos session have 2% quota remaining. Is there useful work you can divide
between the two sessions and complete, commit and push to main before exhausting quota?" The work was divided by the
planner's ranking within each account's budget, and both sessions' branches land in this one train.

**Cluster 1048A (Mythos lane, account 1, Opus; PB2757, MAJOR wrong answer, silent).** `SequentialConnector.OpenReader`
built its `StreamReader` with `detectEncodingFromByteOrderMarks: true`, so a file whose first bytes looked like a byte
order mark was silently re-decoded: a record-sequential file whose first record starts X'FFFE' (a `PIC S9(4) COMP`
holding -2) was read as UTF-16, and a UTF-8-BOM line sequential file lost its BOM bytes from every REWRITE anchor. The
channel is Latin-1, one char per medium byte; the BOM rule stays `NextFrame`'s alone. Golden
`tests/conformance/2023/pb2757_medium_bytes_not_bom_sniffed.cob` (first records X'FFFE', X'FEFF', X'EFBBBF', and a
UTF-8-BOM line sequential REWRITE). PB2757 landed.

**Cluster 1049E (operator, account 2, Sonnet; PB2739).** The rule for which division a text is in was written twice and
the copies disagreed (the main-source fixed-form walk saw a division header only in Area A; library text took any word
ending in -ID; each block restarted the cursor). `DivisionCursor` is now the one rule, read from text-words wherever they
begin on a line (§6.3.1, the program-text area). Three 1985 goldens and `DivisionCursorTests`. PB2739 landed.

**Cluster 1049A (operator, account 2, Opus; PB2844, MAJOR rejects legal source).** `01 K CONSTANT AS LENGTH OF M OF V`
with `V TYPE T` was refused with COBOLNET1547 although §13.18.57.4 2) a) gives V the subordinate M (`cite.py --check`
OK). `ComposeAhead` is factored out of `CompleteDescriptionAhead`, and `ComposePendingSubjectsAhead` runs when a lookup
fails; the described-later test now checks qualifiers as well as the data-name. Two 2002 goldens. PB2844 landed; the
cluster's PB1941 (both arms still reproduce) and PB2051 (waits on PB1784) stay open, re-probed; the implementer's lead
is filed as PB2867 (a being-described qualifier gets COBOLNET1547 instead of COBOLNET0899; process_only, not re-run at
filing). Lead block PB2867-PB2871: PB2868-PB2871 unused.

**Landed by the operator** (the loop is stopped at the weekly cap): the three cluster commits rebuilt with
`git commit-tree` on the implementers' exact trees with forensic messages, the lander's whole-population gate, the
Linux gate, push-main. Wave 1049 used 338 K subagent tokens for its two groups.

