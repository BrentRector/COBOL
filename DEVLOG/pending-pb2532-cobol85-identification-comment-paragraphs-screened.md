## PB2532: the COBOL-85 identification comment paragraphs are screened once each and in sequence

**Re-probe.** On the branch's own build, `AUTHOR. A.` then `AUTHOR. B.`, and `SECURITY. X.` before `AUTHOR. A.`,
compiled clean at `--std 85` (EXIT 0). The `ClauseCardinalities` identification-division row ranked the one element
the 2023 figure (§11.2.1) prints, `[ options-paragraph ]`, and left the five COBOL-85 comment-entry paragraphs
uncounted, so no edition screened them.

**The mechanism, and the shape.** The table held one figure per list, the 2023 one; a list whose earlier edition
printed other elements had no way to say so. `ClauseList` now carries an optional `Earlier: EarlierFigure(Until,
Figure)` chain and `At(year)` returns the figure in force at an edition, which `ClosedFormatPass.ScreenClauseRepetition`
asks with the targeted edition. The identification-division row's earlier figure (below 2002) ranks AUTHOR,
INSTALLATION, DATE-WRITTEN, DATE-COMPILED, SECURITY, and leaves OPTIONS (a 2002 introduction, COBOLNET0804 at 85) and
REMARKS (the COBOL-74 carryover the 1985 figure does not print) uncounted. A row's `Clause` became `Format`, the
authority a diagnostic names ("ISO §11.2.1" or "ANSI X3.23-1985"), and `UnrepeatedElements.AtMostOnce` takes the same
text (its RD and PAGE callers now pass "ISO §13.14.2" / "ISO §13.18.39.2"), so the COBOL-85 figure's COBOLNET2423 and
COBOLNET2987 never cite an ISO clause that does not print it. Every ISO row's message is byte-identical.

**The figure is derived, not quoted.** The repository holds no 1985 text (VERSION_CHANGE_REFERENCE ratified decision
#1). Each paragraph sits in its own bracket with the ellipsis on the comment-entry; the 38 NIST CCVS-85 programs that
write them keep this sequence, once each, and a scan of the 5,958 sources under `tests/` found none that repeats or
reorders them. Row 7.10 records the derivation and how to overturn it.

**Goldens.** `85/pb2532_identification_comment_paragraphs_85` (all five, once each, in sequence, one comment-entry over
two lines: compiles and runs); `negative/pb2532-comment-paragraph-repeated` (COBOLNET2423) and
`negative/pb2532-comment-paragraph-out-of-order` (COBOLNET2987), both `reject-at: 85`. `ClauseCardinalityDriftTests`
now holds every figure of a chain to the row obligations and pins the edition edge.

**Sibling sweep.** The other closed lists whose 1985 figure differs were checked: the FD, SD and data description
entries count their removed '85 clauses once-only at every edition already, and the CONFIGURATION SECTION ranks the
same paragraphs. The OBJECT-COMPUTER paragraph is the one other arm: at 85, `MEMORY SIZE` twice, or `SEGMENT-LIMIT`
before `MEMORY SIZE`, compiles clean. It is a reviewed exemption of the table, and its 2023 choice-indicator clauses are
counted by `DataBinder.SwitchBindSpecialNames` (COBOLNET1652), so screening its 1985 figure from the table would report
PROGRAM COLLATING SEQUENCE twice. Which reader owns that count is a design decision, reported as a lead.

**Rows.** None: the rule is the ANSI X3.23-1985 figure, which carries no row in the 2023 inventory (`closes_rows: []`
with its reason).
