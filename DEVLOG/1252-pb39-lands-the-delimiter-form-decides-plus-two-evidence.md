## Entry 1252 — 2026-08-08 22:35 PDT — PB39 lands: the delimiter form decides, plus two evidence rules the mechanism note lacked — 257 ids re-keyed, six false rules demoted, the count contract held

The first catalog campaign is done. The DEVLOG-1167 mechanism ("whatever form a block opened with is
its top level; a change of form opens a nest; a return closes it") was implemented as a level stack in
extract_rule_catalog.py — but only after simulating it over the WHOLE transcription first, which is
what the two reverted attempts never did, and the simulation proved the mechanism note incomplete in
two ways the four recorded clauses could not show:

1. FORM DRIFT: the transcription changes delimiter form mid-run at the TOP level — §13.18.40.3 writes
PICTURE's SR 1–5 as `N.` and SR 6–37 as `N\)` (also §7.2.3.3, §10.7.3, §13.18.14.3, §13.18.62.4). The
pure delimiter rule demoted 32 correct ids in that one clause. So: an ordinal that strictly continues
an open level's run (n = last+1) belongs to that level regardless of form, form-matched levels winning
when two continue with the same n (§9.3.6 has a paren list and a dot nest both ending at 3).

2. FLATTENED NESTED TAILS: the transcription indents the HEAD of some nested lists and flattens the
TAIL to column 0. §14.9.13.4 GR4a's list is items 1\.–3\. indented, 4.–6. at column 0 — so a column-0
ordinal continuing the most recent INDENTED run is a nested item, not a rule. This found SIX rows the
committed catalog filed as top-level rules that are not rules at all (GR-14.9.33.4-3, GR-14.9.37.4-3,
RV-15.30.3-3, RV-15.31.3-3, RV-15.95.4-5, RV-15.95.4-6 — §15.95.4's own SINGULAR heading "Returned
value rule" corroborates), and in §14.9.33.4/§14.9.37.4 it hands the id `-3` to the standard's real
rule 3, which had been sitting in a sub-list behind the impostor.

The migration was computed by TEXT-KEYED diff of old catalog against new (segmentation never touches
rule text, so text+order is the stable key): 3,981 rules before and after — the note's count contract
held exactly — 257 ids changed (239 promotions to standard numbering, 6 demotions, 12 sub-list
rekeys). Re-measuring the adjudicated exposure found FOUR rows, not the note's two: RV-15.30.3-3
(DIVERGES) and RV-15.31.3-3 (PARTIAL) were adjudicated in the R-wave after the note was written.
All four migrated; build_inventory carried 421/421 verdicts with 0 added / 0 removed; GAP 3,903
unchanged; SpecTraceabilityInventoryDriftTests 10/10.

The drift guard: every column-0 ordinal must now be EXPLAINED (level continuation · indented-run
tail · same-form restart · list opening at 1); anything else is reported per-clause and fails
--check. Zero unexplained across the transcription, and the guard was proven to fail first — a
`77\)` injected into §15.68.3 produced exit 1 naming the clause and ordinal — before being trusted.

Residue filed as PB55, not ridden in: whether a nested item is a catalog ROW currently depends on how
the transcription indented it (§14.9.13.4's L2 holds items 4–6 of a six-item list whose items 1–3 are
invisible inside GR4's text). That is a denominator question — the class D13 reserves for the owner —
queued beside PB29's batch rather than smuggled into an identity fix.

