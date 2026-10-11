## Entry 1447 — 2026-09-03 11:39 PDT — Two owner decisions land together, and each one’s own note carried a premise the tree contradicts

Two answers the owner gave on 2026-09-02, implemented in one worktree because they share a mechanism: the A.4
declined-optional-element surface. GAP 3119 → 3106. No new diagnostic code — COBOLNET1728 stays free.

**PB371 — the vacuously-satisfied rule.** A syntax rule that constrains a CLAIMED statement but whose antecedent
can only be created by a DECLINED module records **CONFORMS, witnessed by a test pinning that the antecedent is
refused**. The antecedent here is the APPLY COMMIT clause (§12.4.6.3, Annex A.4.3 item 2), refused by name with
COBOLNET1709 since witness cluster B2 — and PB371's own note still recorded the pre-B2 measurement, "a parse
error at every edition". That staleness mattered: the named refusal is precisely what makes the owner's answer
available, so the note read as if the question could not yet be answered.

The brief said fourteen rules. It is **seven**, and the `commit-and-rollback-only` selector's prose already said
so — *"Seven of them have no surviving content — their antecedents are simply unsatisfiable."* The other seven
state their content under the COMPLEMENT ("Except when the file is specified in…", "is NOT subject to an active
APPLY COMMIT clause"), which under the decline is the ONLY state this implementation has, so their content is
live and their verdicts speak about CLOSE, DELETE, OPEN and the fatal-exception path; three of them already
carry PARTIAL or DIVERGES. Recording all fourteen would have asserted that seven live rules were vacuous.

Each of the seven got **its own** negative fixture rather than sharing the existing `declined-apply-commit`.
That is the load-bearing half: a witness that only wrote APPLY COMMIT alone would keep passing the day A.4.3 is
claimed, leaving seven CONFORMS verdicts standing on an antecedent that had become reachable. Each fixture
writes the rule's OWN forbidden construct beside the refused clause — the LOCK MODE clause, the SAME area, the
OPEN sharing phrase, READ WITH LOCK, REWRITE WITH LOCK, UNLOCK — so it stops failing exactly when its row needs
re-adjudicating. Self-review caught that the SAME clause needs TWO of them: SR-12.4.6.4.3-11 forbids mixing a
commit-subject file with one that is NOT subject, while GR-12.4.6.4.4-3 says "another SUCH file", i.e. both
subject. One fixture would have given GR3 a witness that never writes its own antecedent — the very property
these fixtures exist to supply.

⚠ **A citation passed `--check` against the wrong rule.** `--check 14.9.30.3 "none of the phrases IGNORING LOCK,
WITH LOCK, or WITH NO LOCK shall be specified"` returns **OK** — on rule b)/4, whose antecedent is "If automatic
locking has been specified for file-name-1", a REACHABLE condition with live content and the exact opposite of
what the row claims. Only the full antecedent lands on rule 5. Two adjacent rules forbid the same three phrases;
the quoted fragment cannot tell them apart. A `--check` verdict is evidence about the TEXT, never about which
rule you meant.

⚠ **And two fixtures were wrong in ways that looked like compiler bugs.** `OPEN INPUT F SHARING WITH ALL OTHER`
is the wrong phrase order — §14.9.27.2 puts `[ sharing-phrase ]` before the FILE LIST — and `UNLOCK F ALL
RECORDS` is not ISO at all: §14.9.47.2 is `UNLOCK file-name-1 [ RECORD | RECORDS ]`, RENDERED at folio 768,
square brackets and no `ALL` (that is a GnuCOBOL extension). Both drew a bare COBOL0001, and both first read as
"we reject legal source". The compiler was right twice; the corrections are written into the fixture headers.

**PB375 — the CLASS clause is declined with VALIDATE.** §13.18.11 is the one clause of the §13.16.2
validation-clauses group Annex A.4 never lists, so its disposition needed the owner: if it is not optional,
A.4.1 obliges us to ACCEPT `CLASS IS NUMERIC` and declining it rejects legal source. The deciding ground is
§13.16.2 itself, RENDERED at folio 364 — the printed Format-1 group opens with `[ class-clause ]` and its
meta-language table maps it to "13.18.11, CLASS clause" — with §13.18.11.1 leaving the clause no content outside
the module. One grammar alternative, one descriptor sentence, one selector arm, one witness, and no new code:
the selector went 75 → 81 rules and the six new ones are exactly §13.18.11's, so zero adjudicated rows moved.

⛔ **The derived clause-namer failed its first real test, and that is why the operand is a sub-rule.**
`DeclinedFacilityPass.ClauseName` names a declined clause by its leading TERMINAL run — the terminals before the
first sub-rule — which is what lets a new alternative need no code at all. Every earlier alternative took
operands that were ALREADY sub-rules (`literal`, `dataReference`, `condition`), so the contract had never once
been exercised. CLASS's operands are the reserved words NUMERIC / ALPHABETIC / ALPHABETIC-LOWER /
ALPHABETIC-UPPER, and the obvious inline alternation would have produced *"the CLASS NUMERIC clause"* — a
diagnostic that renames the clause after whatever the user wrote. `validateClassOperand` exists for that reason,
it is also the faithful transcription of the printed brace group, and the contract is now stated in the code and
pinned by a test asserting the message does NOT contain "CLASS NUMERIC clause".

⛔ **And PB375's own note had the edition premise backwards.** It said CLASS is a user word below 2002 "which a
positive control must keep true", and proposed `01 CLASS PIC X.` at `--std 85` as that control. Measured against
`reserved-words.json`: `"CLASS": {"r85": true, … "continuous since 1985"}` — CLASS is reserved at ALL FOUR
editions, because §12.3.7's SPECIAL-NAMES CLASS clause has always existed, and that program is legal at NO
edition. The gate is still right, for a different fact: the DATA-DESCRIPTION clause arrived with VALIDATE at
2002, so at `--std 85` the entry is a construct the targeted edition does not have and the honest answer is a
syntax error rather than the name of a facility. The controls now assert THAT, plus the hazard the shared word
really creates — §12.3.7's own CLASS clause still compiles and RUNS. `CobolDeclined.g4`'s header splits the
edition-gate rationale into its two distinct reasons, and a second copy of the old reason further down the same
file was deleted rather than left to disagree.

Chasing the premise turned up one defect neither decision covers, registered rather than fixed: the §8.9
reserved-word funnel's COBOLNET0901 only reaches words `dataName` admits, so `01 CLASS PIC X.` and
`01 ALPHABETIC PIC X.` still draw a generic COBOL0001 plus a misleading "A period may be missing", where
`01 DESTINATION PIC X.` gets the named answer. The population is every §8.9 word with a lexer token and no
`cobolWord` membership, and the fix is not to widen `cobolWord` — that would make them legal user words.

Also corrected in the same change set, because both described a compiler two landings old: `CONFORMANCE.md` §5
rows A.4.3 and A.4.14 still said the APPLY COMMIT clause "has no grammar surface at any edition and is a generic
syntax error" and that the VALIDATE data-division elements "have no arm in `dataDescriptionClause`". Both have
been named refusals since cluster B2, and both owning notes (PB261, PB283) are landed.

Landed by squash-merging the implementer's worktree branch (eight WIP checkpoints; base `ac2dadb8`;
`DiagnosticCatalog.cs` auto-merged over the PB379 landing). The implementer's gates in its worktree: build 0/0,
FULL Conformance 5525/5525, Unit filter 95/95, every new negative and fact confirmed executed by name. Wave-local
gate on main, filter `~Negative|~Corpus|~Declined|~CobolWords|~DerivedVerdict|~SpecTraceabilityInventory|
~DefectiveRowCoverage|~VersionMatrix`: both audits at zero, Conformance 3429/3429, Unit 5294/5294,
characterization 33/33, `=== WAVE-LOCAL GATE: GREEN ===`. The funnel finding is `PB382`.

