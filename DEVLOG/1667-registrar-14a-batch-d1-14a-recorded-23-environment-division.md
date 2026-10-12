## Entry 1667 — 2026-09-24 04:03 PDT — Registrar 14a: batch d1-14a recorded (23 environment-division / file-control subjects), 29 notes filed (PB1072–PB1100), GAP 2026 → 1989

**What.** Registrar 14a merged, clustered and recorded adjudication batch d1-14a — the 23 subjects of the
environment division (§12.2–§12.3: configuration section, SOURCE-/OBJECT-COMPUTER, SPECIAL-NAMES p1–p3,
REPOSITORY) and the input-output section (§12.4: FILE-CONTROL entry and its clauses, ALTERNATE/RECORD/RELATIVE KEY,
COLLATING SEQUENCE, FILE STATUS, LOCK MODE, ORGANIZATION, RESERVE, SHARING, SAME, APPLY COMMIT). The adjudicators
worked on the pinned tree `adj-d1` @68bbdb00f; trains 58/59's src diff since the pin was checked and fixes none of
the findings.

**Shape.** 168 rules adjudicated; 6 NEEDS-OWNER-DECISION rows excluded (PB1099); 163 records written (162 from the
merge plus FMT-13.4.5.2, re-verdicted CONFORMS → DIVERGES on the general-slug adjudicator's measurement — the FD
grammar still admits ORGANIZATION/ACCESS MODE/RECORD KEY and ignores them). Histogram: CONFORMS 78 (38 closed the
GAP, 40 test-needed — the golden lane's next input), DIVERGES 49, PARTIAL 23, NOT-IMPLEMENTED 13. **GAP 2026 → 1989**
(+37 closed of 4,348). Eight refuter verdicts replaced the adjudicator's: seven toward the stricter verdict (CONFORMS
→ PARTIAL on GR-12.4.5.3-4, GR-12.4.5.10.3-1/-4/-5, GR-12.4.5.12.4-4, GR-12.3.8.4-12 and SR-12.3.7.3-L7.4, which
opened PB1097, PB1098, PB1100 and PB1091's L7.4 arm), and one free-text "CONFORMS (test-ref must be
empty…)" (GR-12.3.6.4-10) recorded as CONFORMS with an empty test-ref. None was overturned toward leniency.

**Shape violations fixed at the source.** 132 records carried commentary inside `editions` ("2023 (the NATIONAL
phrase … enters at 2002 …)") or parentheticals / `Type.Member` symbols inside `code-location`; the commentary moved
verbatim into `notes`, the fields kept only their schema shape. The first gate run caught 16 dotted symbols
(`CodedCharacterSet.CharAt`) and one wrong grammar path the shape regex admits but the resolver does not — fixed in
both the batch and the inventory, gate re-run green.

**Mechanisms (28 new defect notes plus the owner note PB1099, one per root cause, each a fill unit via `cluster:`).**
- PB1072 SUPPRESS WHEN literal-1 mis-decoded (figurative kept as its spelling, no space-extension) and SR7 unscreened — wrong answer.
- PB1073 FileControlKeyRules still excludes §12.4.5.6.3 SR3/SR4 and §12.4.5.12.3 SR3 on a stale "machinery" claim.
- PB1074 file-control collating resolution is key-class-blind and drops alphabet-name-2; lazy SR1/SR2, no SR7 — spurious '22'.
- PB1075 file COLLATING SEQUENCE grammar refuses a qualified data-name-1 and admits a repeated FOR alternative.
- PB1076 OO environment-division placement: the method arm refuses a legal bare header; class-level/interface I-O section and FACTORY/OBJECT/INTERFACE SPECIAL-NAMES lists unscreened (with PB813).
- PB1077 SELECT with no FD crashes the run unit at OPEN; duplicate SELECT silently replaces the first.
- PB1078 ASSIGN literal-1: zero-length accepted, hexadecimal-alphanumeric refused.
- PB1079 EXTERNAL file fingerprint omits RESERVE, COLLATING SEQUENCE, key descriptions/locations, SUPPRESS WHEN.
- PB1080 FILE STATUS operand never screened (SR1–SR4): process kills, a Roslyn crash, silent truncation.
- PB1081 FD grammar admits file-control clauses and ignores them.
- PB1082 PROGRAM COLLATING SEQUENCE alphabet-name-1 accepted when undeclared.
- PB1083 REPOSITORY intrinsic-name screen: membership ignores >>COBOL-WORDS and the edition window; reached by six funnels only.
- PB1084 UserFunctionsOf remaps a function's own self-named specifier (SR11) — legal recursion refused.
- PB1085 inherited REPOSITORY clause numbers in code, tests, a user message and constructs.json.
- PB1086 no §8.13 external repository and no determination (blocked on PB1099 Q3).
- PB1087 no SAME-clause validator (SR2–SR10, one-operand clause).
- PB1088 contained program loses its container's WITH DEBUGGING MODE (1985 wrong answer).
- PB1089 currency literal-7's class discarded, SR28 unenforceable.
- PB1090 `SPECIAL-NAMES. .` (empty paragraph, SR31) is a parse error.
- PB1091 SPECIAL-NAMES ordinal decoder: int.TryParse overflow silently drops a symbolic character / misnames the rule; SR17 c4 applied to a numeric ordinal.
- PB1092 undocumented A.1 determinations the code makes (items 43, 186; lock-mode 153–155 and item 8 measured).
- PB1093 HIGH-VALUE and the collating fingerprint pinned to Latin-1 while the repertoire is UTF-16.
- PB1094 DYNAMIC LENGTH STRUCTURE never realized though WRITE serializes the item — silent corruption (row moved from PB991).
- PB1095 CodedCharacterSet.CharAt answers one character per ordinal for CLASS … IN (GR7 k6).
- PB1096 APPLY COMMIT zero-operand spelling refused anonymously.
- PB1097 sort-merge file's ASSIGN USING content never associated (DOC-A.1-73 at OPEN only).
- PB1098 keyed store opened I-O through a sequential FD: REWRITE overwrites the header, destroying the organization.
- PB1100 a CLASS definition's REPOSITORY user functions are refused at every method reference (COBOLNET1505).

**Extended, not duplicated:** PB756 (+FMT-12.4.5.6.2, the RELATIVE KEY false-leniency comment), PB749
(+GR-12.4.5.8.4-1), PB755 (+FMT-12.4.5.9.2), PB643 (+GR-12.4.5.14.3-1), PB813 (SOURCE-/OBJECT-COMPUTER halves of
SR3), PB994 (+SR-12.4.5.5.2-1), PB791 (+SR-12.3.7.3-18/-19, `wrong_answer` flipped true: `CURRENCY SIGN IS SPACE
PICTURE SYMBOL "#"` prints `SPACE1.50`), PB991 (GR-12.3.7.4-19 moved to PB1094), PB917 (file-control clauses written
twice), PB372 (this batch's dossier gaps). PB773/PB781 already claimed their rows.

**Owner questions (PB1099, one per decision):** Q1 the record-key-name SOURCE form (Annex A.3 item 40) —
DOCUMENTED-NON-SUPPORT for its six rows?; Q2 FMT-12.4.6.3.2 (Annex A.4.3 APPLY COMMIT) — DOCUMENTED-NON-SUPPORT?;
Q3 §8.13 external repository — implement, or document a determination (A.1 66/161/162)?

**Friction.** The shared scratchpad was being written by another workflow agent (`rows.py` overwritten mid-run);
this registrar's scripts moved to a private `r14a/` subdirectory. `merge_batch.py` printed "owner-decision excluded:
7" where six rows were absent from the batch — recorded, not chased.

**CI red, fixed before landing.** The first push (761d326a7, run 35991662728) went red on two Unit drift gates the
registrar's filtered gate did not include: `FileControlKeyRuleDriftTests` (an adjudicator code-location credited
SR-12.4.5.2-11/-13 to `FileControlKeyRules.cs#Screen`, which does not screen them — re-sited to `BindFileControl`) and
`ClosesRowsBackLinkDriftTests` (landed PB829 claimed FMT-13.4.5.2, now DIVERGES — the claim withdrawn, PB1081 owns
the row). Lesson: a registrar that re-verdicts or re-sites rows gates on the WHOLE Unit assembly, not the three
inventory filters — every drift test that reads the inventory is a consumer of the batch.

