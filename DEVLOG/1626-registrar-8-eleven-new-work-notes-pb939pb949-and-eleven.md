## Entry 1626 — 2026-09-21 13:42 PDT — REGISTRAR #8: eleven new work notes (PB939–PB949) and eleven extended — two of them were already sitting in another note's PROSE, three orphan rows found owners, and one reported defect turned out to be fixed and another to be stale

A lead-filing pass over everything that landed after registrar #7: the six **wave-42 implementer reports**
(PB455, PB415, PB395, PB587, PB394, PB388), the **train-43 lander report**, the **battery-81 record** and
**DEVLOG 1622–1625**. Twenty-two leads. **Every one was grepped against `kb/Work/` FIRST**, every probe was
REBUILT and RE-RUN on this worktree's own `dotnet build CobolSharp.sln -c Debug` (0 warnings, 0 errors) at main
`c774def44`, and every citation was re-derived with `python scripts/spec/cite.py --check` rather than inherited.
No compiler code changed and no diagnostic code was allocated — a registrar changes neither. Eleven leads became
notes, eight were folded into notes that already owned them, and three were dropped because the measurement said
so.

**⛔ Two of the eleven were already written down — inside `kb/Work/PB933`'s BODY.** That note carried a paragraph
headed *"⚠ Also found, NOT this note's mechanism and reported for filing"*, holding two findings: that
`VALUE NULL` is accepted on a data item of any non-pointer class, and that `DataBinder.Switches.AlphabetFigurative`
is an eighth private reading of §8.3.3.6.2. Both are real, both reproduce, and neither was visible to
`work.py next`, to `kb/Work.base`, or to any ranker — because prose is not a register entry. That is CLAUDE.md
rule 8's own worked example happening again, and it is the argument for the rule: a defect in a paragraph is a
defect nobody is going to fix. They are now **PB939** and **PB940**, and PB933 says where they went.

**The two measured wrong answers are both in MOVE, and both were found by probing an axis a CONFORMS row's
witnesses held fixed.** `PB943`: a zero-length NATIONAL group sender leaves `PIC N(4)` holding
U+0000 U+0020 U+0000 U+0020 — the BYTE image of two national spaces written into four CHARACTER positions — where
§14.9.25.4 GR2 substitutes SPACE. `MoveClassifier.ZeroLengthItemRoute`'s receiver filter is
`Numeric or NumericEdited`, and its doc comment states that filter as a **PROOF**: *"for every other category the
two readings are the same store … space for alphabetic / alphanumeric / national and their edited forms"*. The
measurement contradicts the proof, and the row it rides, `GR-14.9.25.4-2`, reads CONFORMS.
`PB944`: a NON-zero BIT group moved to `PIC 9(3)` stores U+0080 and one space — two characters where three digit
positions are owed, `FUNCTION LENGTH` still answering 3, and `IF R-NUM = ZERO` then TRUE. §14.9.25.4 GR4 makes it
an ELEMENTARY move and §14.9.25.3's Table 16 decides whether the pair is admitted at all; neither reading permits
storing the raw bit byte, which is why it is a defect note and not an adjudication. Its row, `GR-14.9.25.4-4`,
also reads CONFORMS. Both were measured BYTE-EXACT with `od -c`, because the source report's console rendering of
the second one — *"prints `BITNZ=`, zero characters"* — is not what the item contains.

**`PB941` is the largest new note and it claims seven rows nobody had.** `>>PUSH` and `>>POP` are parsed,
operand-screened and then DISCARDED: they have no state semantics at all. Measured with **both controls run**,
so the negative result is evidence rather than an absence — `>>TURN EC-BOUND-SUBSCRIPT CHECKING ON` alone aborts
the run unit on a bounded reference, `CHECKING OFF` alone does not, and through `ON / PUSH ALL / OFF / POP ALL`
the OFF state **survives the POP** and the program completes. §7.3.22.4 GR2 saves the state of every directive bar
five; §7.3.20.4 GR3 restores them. An unsuccessful POP is silent although §7.3.20.4 GR2 requires a warning and
Annex A.1 item 140 marks that item **required**. `GR-7.3.20.4-1/-2/-3`, `GR-7.3.22.4-1/-2/-3` and `DOC-A.1-140`
were all GAP with an EMPTY verdict and **no claimer at all**. The note also says, in terms, that the fix is not
"push and pop `TurnState`": GR2's subject is a SET of directives, so the supportable shape is one
directive-state container with save/restore as operations on it, plus a drift test that every catalogued
directive bar GR2's five is in the pushed set — CLAUDE.md rule 5, and the estimate in the source report corrected
rather than met.

**Three leads did not survive the re-run, and that is the pass's most useful output.** ⭐ The PB455 report's
§13.18.63.3 SR6 Format-3 asymmetry — filed as a new defect AND as an owner determination — is **CLOSED by
PB921**, which landed in train 43 between the report and this pass: `01 X PIC ZZ9.99.` / `88 X-TEN VALUE 10.`
draws COBOLNET0900 at `--std 2014` and composes `X=[ 10.00]` at 2023, and PB921's own DETERMINATION records the
counter-argument in full so the question is not rediscovered. Filing it would have sent an implementer at working
code. ⭐ `PB498`'s `value-clause-p2` mechanism is **STALE**: its subject, the national-through-range COBOLNET0899
staging, was deleted by PB761 in landing train 18, and the range now compiles and runs. ⚠ My FIRST probe of that
was itself wrong — it declared `ALPHABET NAT1 IS NATIVE`, an ALPHANUMERIC alphabet, and so drew COBOLNET1999
(§14.9.13.3 SR3), a different and correct screen doing its job; the `FOR NATIONAL` spelling is the measurement,
and the mistake is recorded rather than discarded. ⭐ And `SR-14.9.39.3-10`'s stated reason — *"`objectReferenceUsage`
cannot spell a FACTORY-phrased class sender or ACTIVE-CLASS"* — is no longer true: `CobolOO.g4:224` spells all
three alternatives since PB496 landed. **A row's `notes` field is prose, the code site it names changed, and no
gate compares the two**; the row went to `kb/Work/PB929`'s golden round as a clean lead-off case, since its fix is
a witness and a verdict rather than code.

**`PB946` is a rejection with no rule behind it.** `IMPLEMENTS I-1 I-1` in an OBJECT paragraph draws COBOLNET0840
citing §11.8.3 SR1. §11.8.3 was read WHOLE from the standard, because the finding is an ABSENCE and an absence
cannot be `--check`ed: it has exactly two syntax rules, SR1 requires the name to be in the containing class's
REPOSITORY paragraph — which the probe satisfies, and is the only reason resolution succeeded and control reached
the duplicate arm — and SR2 is a method-prototype conformance rule. Neither forbids naming an interface twice,
and §11.8.2's printed `IMPLEMENTS { interface-name-1 } …` is §5.2.6's repetition. `audit_code_citations --check`
passes it because §11.8.3 SR1 exists. Train 43's PB838 fixed the THREADING of that citation into the arm; nobody
asked whether the arm should fire.

**Two notes are about a table whose predicate is narrower than its rule.** `PB947`: §13.16.3 SR24 c)'s
"alphanumeric group" is a TWO-exclusion test (`IsGroup && GroupUsage is None`) where §3.11 excludes FOUR kinds, so
a VARIABLE-LENGTH group is refused under letter **c)** and quoted the standard's sentence for a rule it does not
violate — while letter **h)**, "A variable-length group", is already in the same table two rows down, where
first-match order never reaches it. The verdict is right and the citation is wrong, which is the same shape as
PB946 and as PB932's population. It took **five probes**: the first four placed the level-88 after the last
MEMBER rather than after the GROUP entry and so never reached the exclusion at all. The strongly-typed half is
recorded as **NOT measured**, because rows g) and §13.18.57.3 SR2 may refuse it first and the fixer must probe
before assuming. `PB945` is the same shape in the Place model: six `not (RefModPlace or RenamesPlace)` tests are a
hand-written TYPE LIST asking "is this operand's storage one contiguous declared item?", which `PB602`
deliberately left because `DenotedItem` answers a different question — and the census was RE-MEASURED here, since
four of the six line numbers in the source report had moved.

**`PB948` is a number that lives in prose.** `session-probe.ps1` computes the next free diagnostic code as the
ceiling of two scans; both peak at COBOLNET2196 on this tree, so it answers **2197**. The allocator's next free
code is **2203**, because train 43 claimed 2176–2202, used six and RETURNED the rest, and returned codes are
never reused. 2197 is the first of them. The script's own header names the 1573/1518 collision lesson twice and
guards the two OBSERVABLE ceilings against each other — it cannot see the third number, because a returned code
leaves no trace in the catalog, in `src/` or in the generated `DIAGNOSTICS.md`. It exists only as English in plan
§0 and in a DEVLOG entry, and plan §0 is live state the next train rewrites. `kb/Work/PB608` is the same family
from the other side: COBOLNET1535 and COBOLNET1573 are each defined twice today, and the train-43 lander re-found
both. A returned code handed out twice produces exactly that state, so the two are clustered: one gate and one
structure close both.

**Row discipline.** Ten rows newly claimed and three orphans re-homed, each checked against
`tests/version-matrix/traceability-inventory.json` AND against every note's `inventory_rows` first. Seven were
GAP with an EMPTY verdict and no claimer at all (PB941's); three read CONFORMS and are contradicted by the
measurement in the note that claims them — `GR-14.9.25.4-2` (PB425, landed), `GR-14.9.25.4-4` (PB430, landed),
`SR-13.16.3-24` (PB488, landed) — every prior claimer terminal. The three orphans are PB388's, handed off as its
own implementer report asked: `GR-14.9.39.4-12`/`-13` to **PB450**, whose mechanism IS Format 7's missing outer
repetition and whose surface the two general rules depend on; `SR-14.9.39.3-2` to **PB212**, the Format-1 SET
screen whose receiver half is SR1, with the adjudication PB388 flagged recorded INSIDE it as a DETERMINATION on
PB921's precedent rather than escalated, because both readings make today's behaviour wrong;
and `SR-14.9.39.3-10` to **PB929** as above. Nothing was removed from PB388's own list — a row may be co-claimed,
the `DefectiveRowCoverage` gate needs one non-terminal claimer, and dropping a claim is the landing's business.

**Eight leads dropped with the owner named**, every one measured rather than assumed: the drift test writing to a
fixed shared temp path is `PB376` (open, same script and same test — and the PB455 run hit a SECOND instance of
the shape that is not the generator at all: two agents shared one session scratchpad and their gate logs collided,
so the generalisation the note should carry is wider than one script); §13.18.63.3 SR2's unreached RANGE half is
`PB586` (open, the identical mechanism); the two duplicate catalog codes are `PB608` (open, which already names
both); §14.9.49.3 SR3/SR4 is `PB362` (open, which already claims both rows — extended here with the four code
sites the PB395 report measured, and with the observation that SR3 and SR4 point in OPPOSITE directions, so a
one-direction fix passes the other's probe by accident); the DIAG-UNQUALIFIED sweep is `PB388`'s own; PB453 was
excluded by the dispatch as an existing note; and the battery-81 ledger-artifact refresh is an orchestrator chore
already recorded in three places, not a compiler defect.

**Gate**, on the final tree: `dotnet build CobolSharp.sln -c Debug` — 0 Warning(s), 0 Error(s);
`dotnet test tests/Cobol.Net.Tests.Unit --no-build --filter "…SpecTraceabilityInventory|…DefectiveRowCoverage|…DerivedVerdict|…ClosesRowsBackLink"` —
`Passed!  - Failed:     0, Passed:    51, Skipped:     0, Total:    51`; `work.py check` — **982 work items, all
well-formed**; `gen_conformance_notes.py --check` — 15 notes matching the inventory exactly;
`audit_doc_citations --check` — 517 checked / 470 correct / **0 MISFILED / 0 ELIDED**; `audit_code_citations
--check` — rc 0, the non-gating backlog unchanged at 328 (155 DIAG-UNQUALIFIED · 105 RULE · 68 SUBITEM).
**GAP unchanged at 2310 by design** — this pass files defects and moves no verdict, and
`gen_conformance_notes.py` produced no diff.

⚠ **A line-ending trap, checked rather than assumed, and it fired.** `core.autocrlf=true` checks
`kb/Work/*.md` out **CRLF** while the blobs are **LF** — registrar #7 reported this on 2026-09-20 — so a
byte-preserving edit produces a whole-file diff that hides the one line that changed. This pass's edit script
asserted the property before writing and **tripped its own assertion on the first run**, which is the only reason
it was handled rather than shipped: every extended file is now read RAW, edited as LF and written back in the
convention it arrived in. `git diff --stat` for the eleven extensions is 306 insertions / 14 deletions across 22
files, the deletions being the `cluster:` lines widened in place; zero NUL bytes; and the eleven new notes are
pure LF.

