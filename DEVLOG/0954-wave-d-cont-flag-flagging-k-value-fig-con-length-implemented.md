## Entry 954 — 2026-07-21 16:05 PDT — Wave D (cont.): FLAG flagging — k VALUE-FIG-CON-LENGTH implemented (NOT deferred; owner: "we fix issues, do not defer them"); INCR 1 COMPLETE

Owner correction: I had recorded k for a "dedicated increment" — that is technical debt by another name. Fixed it
now. **k VALUE-FIG-CON-LENGTH (§7.3.15.4 GR4 k; E.2 item 11)** — a figurative-constant VALUE (any figurative) on a
data item with NO SPECIFIED LENGTH, decided precisely from the parse tree (the source-anchoring the flag fold needs):

* **no PICTURE clause**, AND
* **no length-implying USAGE** — `UsageGivesNoLength`: DISPLAY (explicit or absent, the default) has no length
  without a PICTURE; every other usage (COMP-*/INDEX/POINTER family/float/binary families) implies a fixed one.
  Probed both live: `01 X USAGE DISPLAY VALUE SPACE.` (no PIC) compiles and IS flagged (genuinely no length);
  `01 Y USAGE COMP-2 VALUE ZERO.` compiles and is NOT flagged (length 8). So excluding *all* usages would have been
  a real false-negative — the keyword is checked (`GetText().EndsWith("DISPLAY")`), AND
* **not a group** — `HasSubordinates`: the immediately-following sibling entry is a real subordinate (level 2–49,
  deeper). A group's figurative VALUE is filled to the subordinates' length, so its length IS specified
  (§13.18.63 SR13). A following 66/88 entry is not a subordinate.

Restricted to real data levels (1–49, 77): a level-88 condition-name reaches `VisitDataDescriptionEntry` via
`valueClause` (confirmed) and is excluded (`IsRealDataLevel`). k reaches ANY item; g/l/j stay numeric-edited-gated.
`VisitDataDescriptionEntry` restructured to read (picture, value, usage) once and branch k (any item) vs g/l/j
(numeric-edited).

**Gate.** Build 0W/0E · CLI-probed the full discrimination set (figurative-no-PIC + ALL + USAGE-DISPLAY flagged;
PICTURE, COMP-2, group, level-88 not — exactly 3/8 flagged) · 34 FlagDirectiveTests (5 new k tests incl. the
COMP-2/group/PICTURE/DISPLAY discriminators) · characterization 33/33 byte-exact. **Detectors done: 8 of 19 —
INCR 1 COMPLETE** (all VALUE + statement options: g/j/k/l/m + FLAG-02 f, on top of Incr 0's READ-PREVIOUS/CLOSE).
NEXT = Incr 2 (the frontend-inline options b COMPILE-TIME-ARITHMETIC-EXPRESSIONS + c EVALUATE, emitted in
`ConditionalCompilationProcessor`) → Incr 3 (state-coupled) → Incr 4 (new-analysis).

