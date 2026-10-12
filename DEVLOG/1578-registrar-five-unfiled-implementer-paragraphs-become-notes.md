## Entry 1578 — 2026-09-09 12:58 PDT — Registrar: five unfiled implementer paragraphs become notes, and one of them was filed as harmless

Three implementer reports (PB792, PB794, PB795) each ended with a "new defects — note-ready paragraphs" section
that was never filed. Seven paragraphs, none of them in the work register, all of them therefore invisible to
`work.py next` — the shape `feedback_measure_the_selectors_complement` is about, one step earlier: a ranker can
only rank what was written down. Five became notes (PB804–PB808) and two were appended to the notes that
already own their mechanisms.

⛔ **The re-probe changed one verdict, and it changed it upward.** PB792's paragraph B filed the `valueItem`
PROPERTY guard as *"Harm: none today"* — a maintainability observation about a hand-written predicate. Measured
on this worktree's own build at `d497f3c7`, it is a MAJOR defect that rejects legal source, and the migration
mode is the thing doing the rejecting. `01 G VALUE N"AB" GROUP-USAGE NATIONAL.` over `05 A PIC N(2).` compiles
clean at `--std 2002` and at `--std 2023`; under `--permissive` at either it draws COBOLNET1639 twice and
COBOLNET1585, because `AdmitsAsUserWord` correctly restores GROUP-USAGE (its user-word verdict is `Removed` —
`r85:false, r2002:true`, so COBOL-2002 took a spelling that was a user word at '85) and nothing then decides
which of the two readings wins. The greedy VALUE operand list takes `GROUP-USAGE NATIONAL` as two operands, the
entry loses its §13.18.29 clause, and a flag whose whole purpose is to accept MORE than the strict compiler
accepts strictly less. §13.16.3 4) licenses the clause order — *"The remaining clauses may be written in any
order"* — so the source is conforming and it is the compiler that is wrong.

Measuring the complement rather than the case turned one word into a population. Of every word that can BEGIN a
`dataDescriptionClause`, seven carry the `Removed` verdict; five of those ride `cobolWord`
(PROPERTY, GROUP-USAGE, CONSTANT, DEFAULT, PRESENT) and exactly one — PROPERTY — has a guard, hand-written
twice at `CobolData.g4:642` and `:698`. The other four were each driven through the real compiler: GROUP-USAGE
is swallowed, while CONSTANT, DEFAULT and PRESENT survive only because their clause tails (`RECORD`, `IS "ZZ"`,
`WHEN N = 1`) cannot continue an operand list, so ALL(\*) prediction backs off — PB792's own finding #1 about
the 31 revoked pairs, restated: they survive on lookahead luck, not on a gate. And the paragraph's claim that
this guard is *"the only per-word permissive disambiguation in the grammar"* does not hold either: there are
three families over eight words (PROPERTY ×2 keyed on `is2002()`, DEFAULT at `initializeOperandList` keyed on
`reservedHere` so the `>>COBOL-WORDS` overlay is honoured, and DELETE FILE's five phrase keywords, unkeyed).
Three sites, three keying schemes, no shared rule — which makes the conclusion stronger, not weaker.

The other four notes. **PB804** — the NIST differential reads exactly one file per program:
`NistDifferentialTests.RunNist:120-125` takes `<testname>.txt`, else one `any-*.txt`, else stdout, normalizes
THAT, and the `finally` two lines below deletes the run directory, so every other file a CCVS program writes is
not merely ungraded but destroyed. It already swallowed a silent wrong answer for a full battery cycle: PB792's
RW104A regression lived entirely in `xxxxx049.txt` while `rw104a.txt` stayed byte-identical to its golden and
battery #61 was ALL GREEN. The shell guard has the same shape, which is the arm a fixer will miss.
**PB806** — `>>ELSE JUNK`, `>>END-IF JUNK` and `>>END-EVALUATE JUNK` compile clean at 2023, because PB794's
`directiveOperand` column keys on the ROW and `if-directive-2002` / `evaluate-directive-2002` each cover three
words with three different operand shapes; both rows say `form: stage`, which is true of the arm that parses a
constant-conditional-expression and false of the two that take no operand at all and ignore `rest` entirely.
Rows `FMT-7.3.16.2` and `FMT-7.3.13.2` claimed. **PB807** — `>>DISPLAY )( 3 +` compiles clean: the row is
`{form: text, operandRequired: true}`, a presence test, where §7.3.12.2 writes a repeated four-way choice plus
an UPON phrase whose braces carry §5.2.6.4 choice indicators; row `FMT-7.3.12.2`, which PB794 correctly left
GAP rather than close on the partial reading. **PB808** — the §7.3.18.2 LISTING figure note still calls ON's
missing underline *"a typesetting omission"* and asserts *"exactly one of ON or OFF shall be selected"*.
Measured from the vector data on PDF page 108 (printed folio 78): `figure_extract.py` reports LISTING and OFF
underlined, ON bare; `figure_geometry.py` reports 0 delimiter stems. §5.2.3 makes a non-underlined word
optional and §5.2.6.3 makes an alternative of only optional words the implicitly selected default, which is
what §7.3.18.3 GR4/GR5 mean by *"specifying or implying the ON phrase"*. It is the ONE of the seven ON/OFF
directive notes the reconciliation LEDGER's group sweep never reached — FLAG-14, LEAP-SECOND,
REF-MOD-ZERO-LENGTH and PROPAGATE all carry the corrected wording, and PROPAGATE's note cites LISTING as its
counter-example, so the transcription now argues against itself across two pages. No compiler defect follows
(PB794 read the underlining off the rendered page, not off the note, and `FMT-7.3.18.2` is CONFORMS), which is
why it is `kind: analysis`; `specs/` is a submodule and was not touched.

The two appends went to the notes that already own the mechanism rather than opening ids for them.
PB795's N1 — `CliExitCodeTests` returning early when the CLI is absent — is [[PB782]], whose title already ends
*"and it passes silently when the CLI it drives is not built"*; the sibling [[PB768]] owns the same FILE and a
different mechanism (the drift guard that blesses an unbounded private launcher), so the append records why the
two must not be merged. What the outside view adds is the answer to PB782's own open objection: PB795 measured
that in this xUnit pin no runner translates `Assert.Skip`'s marker, so there is no honest skip available and
RED-with-the-measured-reason is the correct landing. PB795's N2 — no §9.1.15 file lock for the keyed and
relative organizations — is [[PB771]], and the append records that the instrument finally exists:
`HostCapability.OutsiderCan` makes the assertion one line, PB740's drift class already has the fixture, and
`HostFile.OpenAuxiliary` is still unconditionally `FileShare.ReadWrite` after PB802 moved the store's shape but
not this property.

All five allocated ids were used (PB804–PB808) and none was wasted; the two appends consumed no id, which is
the point of an append and the reason `kb/Work/` was grepped for an owning note before each one was written.
No inventory verdict moved, so there is no `record_verdicts` batch and GAP stays 2733 of 4348 — the three rows
now claimed were already GAP, and claiming them is what the DefectiveRowCoverage invariant is for. Gate: the
inventory / DefectiveRowCoverage / DerivedVerdict leg 47/47 green (its `ClaimsNamingNoRow` check is what proves
the three claimed rule-ids resolve), `work.py check` 840 items all well-formed, both citation audits at zero
(379 verbatim-shaped doc citations, 0 MISFILED; 3712 files scanned for phantoms, 0 findings), and
`gen_conformance_notes.py` regenerated the derived view with nothing to commit.

