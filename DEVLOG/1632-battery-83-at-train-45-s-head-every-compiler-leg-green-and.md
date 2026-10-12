## Entry 1632 — 2026-09-22 06:19 PDT — Battery #83 at train 45's head: every compiler leg green, and BOTH per-case differential flips are conformance FIXES — §8.8.4.4.3 SR4 read from the case, §8.3.2.2 read from GnuCOBOL's own comment, re-baselined; and the Unit population jump the lander blamed on the corpus was PB653's fifth theory

Battery #83 ran at `c982251df` — landing train 45's head, with train 44 under it — in an isolated worktree
already pinned at exactly that commit, never on `main` and never in the shared checkout. The gitignored GPL
GnuCOBOL corpus was copied in FIRST, so `ExternalCorpusPopulationDriftTests` and the rest of the Unit
assembly's external-population tests measured a real corpus instead of being red by absence. One
`bash scripts/battery.sh` invocation, **893 s wall**, artifacts in the session scratchpad (`summary.txt`,
`conformance.trx`, `unit.trx`, `build.log`, `guard.log`, `guard-witnesses.log`, `citations.log`,
`gnucobol.log`, `gnucobol-report.json`, `gnucobol-rebaseline.log`, `gnucobol-rebaseline.json`).

**Every compiler leg is GREEN.** Conformance **7776 / 7776** (0 failed, 9 m 39 s), Unit **28643 / 28643**
(0 failed, 2 m 21 s), characterization **33 / 33**, the guard's evidence-rule witnesses `ALL GREEN` and the
compiler-identity watchdog `ALL GREEN`, NIST **364 MATCH / 0 REGRESSION(S)** against the shipped `cobol`
compiler with `NIST AUDIT: CLEAN` over a declared population of 376, the guard's legacy legs 1203 / 1203 and
503 / 504, `guard-fast` verdict `ALL GREEN`, and all three static citation audits at zero —
`audit_code_citations` 0 findings over 4,656 files, `audit_doc_citations` 0 MISFILED and 0 ELIDED over 529
checked / 479 correct, `audit_evidence_supersession` 0 UNMARKED. **Both test populations reproduce train 45's
own whole-assembly gate EXACTLY** — 7,776 and 28,643, on a different worktree and a different build — which is
the strongest thing a battery can say about a lander's numbers: they are not an artifact of that lander's tree.

⛔ **Battery #82's record required #83 to be "ALL GREEN with 0 flips". It has TWO, and that is written down as
a miss rather than rounded into the green.** `scripts/battery.sh` exits 1 and prints
`=== BATTERY: NOT GREEN (rc=1) ===` on any non-zero flip count by design — the script cannot tell a licensed
flip from a regression, only a person reading the case can — and that is the only reason for the non-zero
exit; no other gate in the run is anything but green. There was no corpus drift (`corpus_sweep` reports
`external population OK: 1323 case(s) == 1323 baseline row(s)`), no NEW or REMOVED case, no case without a
compiler verdict and no harness failure, so the two flips are the whole delta. Totals move
579 / 467 / 236 / 41 → **580 / 466 / 237 / 40**.

**FLIP 1 — `run_fundamental:9066`, `AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT` on COBOLNET0844.** ⚠ This is the
DIVERGENCE-ADDING shape, which is the shape a regression takes in this oracle, so it was read from the corpus
case before anything else was believed. GnuCOBOL's own title is *SPECIAL-NAMES CLASS*. The program declares
`CLASS HEXA IS '0' THRU '9' 'A' THRU 'F'`, `CLASS ODD IS '1' '3' '5' '7' '9'` and `CLASS EVEN IS 49 51 53 55
57`, and then writes `IF NUM-1 IS EVEN` over `01 NUM-1 PIC 9(01)` — a **class-name-1** class condition whose
identifier-1 is of category **numeric**. **§8.8.4.4.3 SR4**: "ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER,
or class-name-1 shall not be specified if the category of the data item referenced by identifier-1 is boolean,
numeric, or numeric-edited." ⛔ That rule number was **re-derived**, not inherited: the sentence was located in
`specs/ISO_COBOL.md` and validated with `python scripts/spec/cite.py --check 8.8.4.4.3 "…"`, which answers
`OK §8.8.4.4.3 4) (Syntax rules)` — **rule 4, not the SR3 the sibling code COBOLNET2202 carries**, and SR3 is a
different rule (usage display or national). The emitting site `ClassConditionModel.cs` already cites
`ISO §8.8.4.4.3 SR4`, so the code and the standard agree. The flip is train 44's PB571 + PB590 landing the
§8.8.4.4.3 operand rules as ONE table: the old screen **returned early unless the operand's category was
boolean**, so SR4 had never been asked about a numeric operand at all. GnuCOBOL's acceptance is their latitude
— their case compiles and runs it expecting success — and the standard forbids the source, so **COBOLNET0844
is the conforming answer**.

**FLIP 2 — `syn_definition:556`, `WE_ACCEPT_THEY_REJECT → AGREE_REJECT` on COBOLNET2213 + COBOLNET2214.** A
CONVERGENCE, and a fix: PB660's screen. GnuCOBOL's title is *Redefinition of program-name by other programs*
and the case declares **two outermost `PROGRAM-ID. prog.` definitions in one compilation group**, plus a
contained `foo` that collides with a COMMON `foo` and a `samename` nested inside `samename` — with the case's
own comments reading "This should cause an error" above each. **§8.3.2.2** (`--check` → `OK §8.3.2.2 2)`):
"Except for method-names and property-names, when two or more source elements identify something with the same
externalized name, they refer to the same instance", and **§8.4.6.3** (`--check` OK): "The names assigned to
programs that are contained directly or indirectly within the same outermost program shall be unique within
that outermost program." We used to accept all of it.

> ⚠ **CORRECTED BY ENTRY 1634 — the complement sentence below is WRONG and is left standing so the error is
> part of the record.** The sweep it reports asked for a SPECIAL-NAMES `CLASS` clause spelled with `IS`, and the
> CLASS clause's `IS` is OPTIONAL. THREE external programs declare one, not one: `run_fundamental:9066`, plus
> `syn_misc:869` (a genuine second declaration, alphanumeric operand, `AGREE_ACCEPT` in both batteries) and
> `syn_misc:7276` (a VALIDATE data-description false positive). Entry 1634 carries the corrected measurement,
> which is STRONGER evidence for the same conclusion. Nothing about the flip attribution or the re-baseline
> changes.

⛔ **THE COMPLEMENT WAS MEASURED, because a screen is evidence about what it fired on and never about what it
passed over.** `corpus_sweep` asserts the population first and then: **exactly ONE of the 1,611 extracted
external programs declares a SPECIAL-NAMES CLASS at all** — `run_fundamental:9066` — so SR4's entire reachable
external population is the case that flipped, and the screen refused nothing else because there is nothing
else to refuse. COBOLNET0844's seven OTHER corpus cases are its older §8.8.1.1 "not a numeric operand" role
(constant expressions, an overlapping MOVE, two MULTIPLY category checks, a non-numeric subscript, a
constant-folding refmod) and **not one of them moved**. For the second flip: **60** external programs carry two
or more PROGRAM-ID paragraphs and COBOLNET2213/2214 fired on the **one** whose names actually collide; 53 NIST
programs are also multi-program and NIST is 364 MATCH / 0 REGRESSION, which is independent evidence the screen
refuses nothing legal. ⚠ **And the batch's other NINE new codes fire on ZERO corpus cases** — COBOLNET2197,
2198, 2199, 2200, 2201, 2202, 2205, 2210, 2211 — stated rather than left as a clean-looking silence: the
external corpus is not evidence about them in either direction, and their evidence is their own goldens.

**The standard licenses both new answers, so the rows were re-baselined** —
`gnucobol_differential.py … --write-baseline` on the same tree. The re-run reproduced the **identical** four
totals (580 / 466 / 237 / 40), so the leg is deterministic on this host, and the resulting diff of
`tests/external/gnucobol-verdict-baseline.tsv` is **exactly two rows**, tier column unchanged at
`DEFAULT_DIALECT`, zero other churn.

⭐ **The Unit population delta is ATTRIBUTED PER CLASS, and doing so REFUTED the lander's own explanation of
it.** Train 45's report put its 28,643 (against train 44's 24,378) down to the GPL corpus having been fetched
into its fresh worktree — "Unit is 28,643 here against their ~24,380 for that reason". Battery #82 ran WITH
the corpus copied in and measured 24,365, so that cannot be the reason; and a per-class diff of the two
batteries' `unit.trx` files says what is: **+4,226 in `FloatLandingModeDriftTests` alone** (16,897 → 21,123),
plus 52 cases spread over thirteen other classes, with no class shrinking and none corpus-sized. The +4,226
was derived from the SOURCE before the trx existed: `LegalShapesAndModes` yields 528 (integer-digits, scale)
pairs × 8 `CobolRounding` members = 4,224 cases per `[MemberData]` theory; the class held four of them plus one
`[Fact]` at #82 (4 × 4,224 + 1 = 16,897) and now holds five plus a `[Fact]` plus a two-case `[InlineData]`
theory (5 × 4,224 + 3 = 21,123) — PB653 added the fifth. `DEVLOG.md` 1631 and plan §0 say only that the fetch
makes the two `ExternalCorpusPopulationDriftTests` real passes, which is true, so no repo document carried the
refuted sentence and nothing needed correcting.

**Batch covered:** everything since battery #82's head `c774def44` — eighteen commits;
`git diff --stat c774def44 c982251df -- src/ tests/` is 216 files, +8,052 / −1,068. **Landing train 44**
(DEVLOG 1630 — the report group's second axis, the class-condition table read three ways, the intrinsic
argument intake; three clusters, nine notes) and **landing train 45** (DEVLOG 1631 — the printed format's
repeated brace, one rendered sentence at every operand, one float landing, one tape phrase and a lock every
connector can SEE, one crossing vocabulary and one definition-name namespace; five clusters, thirteen notes),
plus the wave-42 close and ledger v56 (1628), PB950's `push-main.sh` repair (1629), ledger v58, and battery
#82's own record, which touch no compiler source. Eleven new diagnostic codes (COBOLNET2197–2202, 2205, 2210,
2211, 2213, 2214), 54 new `.cob` goldens of which 34 are negative, `tests/nist/` unchanged, GAP
**2310 → 2276**. Nothing was filed in `kb/Work/`: no red went unattributed, and PB571, PB590 and PB660 were
already `landed` when their trains closed, so no note's status moves.

⚠ **Owed and not done here:** the Conformance Ledger artifact refresh (`python scripts/spec/gen_ledger.py`
plus a publish to the ledger artifact's existing URL, and its `ledger-trend.json` point) for this battery
close. It is recorded here and in the commit message rather than left silent, because the owner's standing
instruction is to refresh after every GAP-moving landing and every battery close.


