## Entry 1120 - 2026-07-29 23:20 PDT - The fabricated citation came from OUR design doc, and a checker that reported 133 defects had 133 bugs

**WHY THIS SWEEP HAPPENED.** PB3's agent report cited "§12.3.7 GR7 k3" for unspecified characters taking "distinct
ascending positions with no gap". I refused to inherit it (CLAUDE.md rule 1) and checked: `cite.py --find
"distinct ascending"` returns **"NO CLAUSE contains 'distinct ascending'"**. The phrase is nowhere in the standard.

**⛔ AND IT WAS OURS.** `COBOLNET_DESIGN.md` carried it TWICE — "§12.3.7 GR7 k1–k6 incl. the k3 distinct-ascending
unspecified tail" and "every unspecified code unit takes its GR7 k3 distinct ascending position". The agent did
not invent it; **it read our design SSOT and quoted it back.** That is rule 1's failure mode running to completion:
a citation propagates from a design doc into a finding, and would have propagated into a fix and a golden. A third
site was in the CA26 LANDED record (`§12.3.7 k)3`).

✅ **THE REAL RULE IS §12.3.7.4 GR7 1.3, and it is STRONGER than the invention** (`--check` OK): "Any characters of
the native collating sequence that are not specified in the literal phrase shall assume a position in the
collating sequence that is greater than that of the highest character specified in this literal phrase. The
relative order within the set of these unspecified characters is unchanged from the native collating sequence."

That **corrects PB3's entry in our favour**. I had written that U+0100's ordinal was "under-specified" and only the
absence of a gap was provable. GR7 1.3 DETERMINES it: unspecified characters sit above the highest specified one
in unchanged native order, so with U+00FF at 255 the answer is **256** and the observed 257 is wrong for a citable
reason. The 255 and the two ALSO characters at 1 are GR7 1.3 and L3.6 exactly — so the substance of the design
doc's claim was right all along and only its citation was fiction. It also names the fix precisely:
`NationalCollatingTable` is already SPARSE over all 65,536 code units and computes the tail arithmetically, which
IS GR7 1.3; the alphanumeric side is a 256-entry array with a `c + 1` fallback. One rule, two implementations, one
incomplete.

**⚠ THEN I BUILT A CHECKER AND IT REPORTED 133 DEFECTS OUT OF 183 CITATIONS.** Not one of the first batch I
inspected was real. Every `§N.N … "quoted text"` pair, demanding the text be inside that clause — and it captured
quotes across markdown blockquote markers, paraphrases attributed to a clause, `§1.6`-style references to a DOC's
own sections, and above all quoted **LABELS** (`"shall-not-with-APPLY-COMMIT"`, `"element; para; line"`) that were
never claims about spec text. A doc uses quotation marks for quoting AND for naming, and nothing in the text
tells them apart. **That check cannot be made precise and is not offered as a gate.**

The precise check is narrower: **the quoted text IS in the spec and is NOT in the named clause.** A label is found
nowhere, so it never fires. That went 9 → 4 → 0 as two further calibrations came out, and BOTH were my bugs:

· the pattern took the FIRST `§` on the line, not the nearest. "never the §13.18.38 window — §14.2.3 GR8 says
  '…'" reported §13.18.38 — a CORRECT citation flagged as misfiled.
· an 18-character floor let defined TERMS through (`"implementor-defined"`, `"the end of the PERFORM"`), and a
  term legitimately appears in many clauses, so locating it elsewhere says nothing. Raised to 30.

This is the figure-audit lesson again (§0: "MY CHECKERS WERE BUGGIER THAN THE TRANSCRIPTION" — 76 findings to 1 as
three tool bugs came out). I very nearly "fixed" a correct citation.

**THE FIVE REAL DEFECTS, each `--check`-validated after correction:**
· `COBOLNET_DESIGN.md` ×2 and the CA26 record ×1 — the fabricated `GR7 k3` → **§12.3.7.4 GR7 1.3**, quoted.
· `COBOLNET_REDEFINES_DESIGN.md` — §13.18.60 GR4/GR11 → **§13.18.60.4**, one level short (the CA37/CA38 shape).
· `PHASE-11-scout-notes.md` — §13.18.62 → **§13.18.63.4 GR4c**, and a stale spec LINE ref dropped.
· `PHASE-13-remaining-waves-scout.md` ×4 — §8.5.3→§8.5.3.1, §13.6.2→§11.9.10.4 GR7, §13.18.63→§13.18.63.3, plus
  two more stale LINE refs.
· `CONFORMANCE-FIX-QUEUE.md` CA4 — the composite quote is ADD's exact §14.9.2.3 SR1b wording; SUBTRACT's
  §14.9.44.3 SR1b differs by a word ("the data item that follow", singular). Reworded so the quote sits with the
  clause that contains it.

**OTHER DOCS BROUGHT CURRENT** for this session's code changes (rule 6):
· `CobolIntrinsics.Exact.cs` said the family is "never double". PB2 made that false as an absolute — a float
  ARGUMENT is legal and now routes to the `…Real` twins. Scoped rather than deleted, since it is still true of
  the argument REPRESENTATION these bodies accept.
· `CONFORMANCE.md` gains the **§15.4.1 implementor determination** PB2 created: when any argument renders
  floating-point, the equivalent arithmetic expression is evaluated in IEEE binary64, with the spec's sign
  discipline preserved (MOD floors, REM truncates). D13 counts required implementor-documentation items, so an
  undocumented determination is a conformance gap in itself.
· The `DA3` entry gains a warning that its "three copies" is **what the symptom exposed, not what exists** — PB4
  found five more.

`scripts/spec/audit_doc_citations.py` is committed with its own false-positive history in the docstring, and
exits 0 across the doc set.

**GATES.** Build clean, Unit **963/963**. Docs + doc-comments only; no behaviour change.

