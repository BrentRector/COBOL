## Entry 1439 — 2026-09-02 22:16 PDT — The citation-repair sweep: ~200 phantom sites over NINE clause
numbers, a gate that makes the next one impossible, and the gate's own two blind spots found by pointing it
at a tree it had never seen

`kb/Work` **PB159** estimated "~10" sites for the nonexistent §8.3.1.2 and **PB182** eighteen for §8.8.4.1.1.
Both were an order out, because nothing had ever asked the question mechanically. The sweep built the tool
that asks it — `scripts/spec/audit_code_citations.py`, with three checks (PHANTOM: the clause does not exist ·
SUBJECT: the cited clause names a DIFFERENT construct than the comment is about · HEADER: a definition-header
comment cites a clause that is not its construct's) — and the answer was **~200 sites over nine clause
numbers the standard does not contain**, repaired here (~296 sites in all once the correct-but-misfiled ones
are counted).

⛔ **A PHANTOM SURVIVES BY ACCRETING MEANING, and §8.3.1.2 was standing in for FIVE propositions.** §8.3.1 is
titled "General" and is one sentence with no subclauses, so every site was unfalsifiable where it stood: the
fixed-point literal's 1–31 digit range (**§8.3.3.3.2**), the two equal-standing quotation delimiters
(**§8.3.3.1**), the doubled embedded delimiter (**§8.3.3.2.3 r3**), the alphanumeric literal itself
(**§8.3.3.2**), the figurative constants (**§8.3.3.6.2 / .3 r1a / .4 r2, r4**), a LITERAL's class and category
(**§8.3.3**), and the characters of a user-defined word (**§8.3.2.1**). The §8.8.4.1.x family was a whole
nonexistent subclause TREE — seventy sites — resolving to §8.5.2.1 + §8.8.4.2.1, **§8.8.4.2.7 r2**,
**§8.8.4.2.16** and **§8.8.4.4**. ⚠ PB182's own proposed replacement, §8.8.4.2.3 SR2, was WRONG (a real clause
about the class of identifiers); the sentence needed is §8.8.4.2.1 — the inheriting failure mode reproducing
itself inside the repair.

**The tool is wired where it will be read**: `scripts/build-local.{sh,ps1}` before the build and
`scripts/battery.sh` PHASE -1, with `--self-test` firing every check on a real defect from this sweep and
then proving it silent on the repair. ⚠ It is deliberately NOT in CI: `build-and-test.yml` checks out with
`submodules: false`, so `specs/ISO_COBOL.md` is absent there and PHANTOM would report SKIPPED — a green that
measured nothing (feedback_green_gates_arent_evidence).

**AND THE GATE HAD TWO BLIND SPOTS OF ITS OWN, which only a tree it had not been calibrated on could show.**
The sweep's baseline was ZERO on its own worktree; on the merged tree it reported **53**, and 50 of them were
the tool's, not the repo's.

1. **It was reading BUILD OUTPUT — 38 of the 53.** The ANTLR build task copies every `.g4` into
   `src/<project>/obj/antlr-lib/`, and `citation_corpus.py`'s `src/**/*.g4` glob swept the copies in. Every
   grammar finding was reported TWICE, and the copy is the grammar as it stood at the LAST BUILD — so a
   repaired citation stayed red until somebody rebuilt, and a clean checkout and a built one disagreed about
   whether the gate passed. A generated file cannot be repaired at all, only regenerated
   (feedback: generated_parser_is_a_build_output). `BUILD_OUTPUT = {obj, bin, Generated}` is now excluded on
   PATH SEGMENTS rather than a prefix, so it holds for any project layout.
2. **SUBJECT treated a golden's whole header as its LABEL — 12 more.** The declaration-header convention
   binds the citation that NAMES the construct; a `.cob` header then goes on to ARGUE, and an argument
   legitimately cites other clauses. `find_string_zero_length.cob` cites §15.59.3, §15.63.3, §15.66.3,
   §15.71.3, §15.72.3 and §15.85.3 precisely to say that those functions DO prohibit a zero-length literal
   and FIND-STRING does not — six correct citations reported as defects. SUBJECT now reads the first
   CITATION-BEARING line; whether a quoted fragment belongs to its clause is `audit_doc_citations.py`'s
   question and existence is still PHANTOM's, over the whole block.

⚠ **And narrowing a selector is exactly where a green stops being evidence** (feedback_measure_the_selectors
_complement), so the self-test grew a `.cob` leg that fires SUBJECT on a golden whose OPENING citation names
the wrong construct, and a clean twin that is silent when a LATER line contrasts with another construct's
clause. Fixing SUBJECT then exposed a THIRD thing: HEADER had been matching `parts[0]`, which on a negative
fixture is the machine directive `*> reject-at: 2023` — no capitalised construct name, no match, **the check
silently disabled on every negative fixture in the corpus**. HEADER now skips the DIRECTIVE line to find the
comment's head, with its own self-test leg proving the widening fires. (An intermediate cut had HEADER share
SUBJECT's citation-bearing line, which made a CONTINUATION line read as a head — `oo_external_file_shared.cob`
wraps "…between a PROGRAM and an / OBJECT (§13.18.22.4)". The two checks want different lines and now get
them.) Three real repairs remained: `kb/Work/PB297`'s last live §8.8.4.1.1, and the two findings THIS
session's own A.4.2 landing had introduced in `CobolScreen.g4` an hour earlier.

⛔ **A REPORT CLAIM THAT WAS FALSE ON THE MERGED TREE.** The sweep's banner read "every site above and every
site in the original paragraph is repaired". It was cut from a base predating the A13 landing, which had
HANDED PB159 a 19-site cluster — `§14.9 MOVE GR4` / `§14.9 GR4`, where §14.9 is the parent *Statements*
clause naming no statement at all. A `grep` on the merged tree still returned all 19. They are repaired here
→ **§14.9.25.4 GR4** (11 code, 2 test, 6 doc), and the neighbour that paragraph flagged as unverified was
CHECKED rather than assumed: `BinderDriver.cs`'s `ISO §14.2.3 GR8` is **correct** — GR8 reads "If the argument
is passed by reference, the activated runtime element operates as if the formal parameter occupies the same
storage area as the argument", exactly the comment's proposition.

**Notes.** `PB159` and `PB182` land. `PB290` — the §8.3.1.2 note cluster C had already opened on main — takes
the sweep's phantom register as its ledger and lands, carrying the `audit-code-citations: names-phantoms`
marker so the register's own quoted phantoms are not read as assertions. The sweep's two DEFECT findings
become notes of their own, and **both were re-measured rather than inherited**:
**`PB299`** (no underscore in a COBOL word) lands **as LANDED, not open** — witness cluster A had already
fixed it, which the sweep could not see: `01 WS_A PIC X(5).`, `>>DEFINE MY_VAR`, `MAIN_PARA` and
`EC-USER-A_B` all compile at 2023 and `--std 85` gives the named COBOLNET0900, all four re-run here. Its root
cause is worth the note on its own: a design doc asserted the ANTLR behaviour "correct COBOL, §8.3.1.2" — a
wrong belief resting on a clause that does not exist — and a TEST was rewritten to a hyphenated name to dodge
the bug, which is CLAUDE.md rule 4 in reverse. **`PB300`** (`paddingCharacterClause`) lands OPEN and **worse
than reported**: the sweep named the over-acceptance (the 2023 standard has no PADDING clause, and the
compiler takes one silently at every edition including 2023), but the same lexer token also makes
`01 PADDING PIC X(3).` a **PARSE ERROR at 2014 and 2023**, where §8.9 does not reserve the word — legal source
refused, the PB301 shape, measured at all four editions here. One token, two arms; the note says the fix must
take both.

**Gates.** `audit_code_citations --check` **0 findings** on 2633 files (from 53) and `--self-test` PASS with
five firing legs and five silent twins; `audit_doc_citations` 192 checked · 147 correct · **6 MISFILED**, the
recorded baseline — it had gone to 7 because the batch-1 registration's `PB317` quoted §14.9.27.4 GR11's
"does not apply to the storage medium" inside a sentence about §9.1.13.2, repaired here by attributing the
quotation to its own clause. Build 0 W / 0 E; Unit `~Citation|~Registry|~Diagnostic|~StorageForm|
~GroupNumericLeaf` Passed 53/53; Conformance `~Corpus` Passed 1224/1224; Characterization Passed 33/33;
`work.py check` 428 items; `audit_annex_a1 --check` no findings; semgrep unchanged against the PB175 baseline
(36 / 474).

