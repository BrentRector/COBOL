## Entry 1588 — 2026-09-09 18:17 PDT — Registrar (third pass): seven notes for the train-26 findings, and two of them are things the sources had wrong

The third registrar pass of the day, resumed from a session-kill that left its first note committed and two more
written but untracked in a dead worktree. Those two — PB826 and PB827 — were rescued by absolute path before
anything else; they were the only copy.

**Registered (ids PB825–PB831 of the PB825–PB832 allocation; PB832 unused).** PB825 DISPLAY of a
BinaryCapacity item renders through the PICTURE's digit count, so `01 B PIC S9(4) COMP-5.` holding −32768 prints
`-2768` while the stored value is right (wrong answer, silent). PB826 `ArithmeticModes.IntermediateExponentRange`
is read under two conventions with nothing enforcing their equivalence, plus a third hand-list of carrier
extremes whose `_` arm hands every future carrier binary64's bounds. PB827 the CLI has no `try` anywhere, so
`cobol x.cob -o <dir>` exits **1** — a code its own published contract does not define — behind an 18-frame
Roslyn trace. PB828 the §13.16.3 SR9 adjudication, both readings written out. PB829 the `genericClause`
catch-all family. PB830 the `computerAttributes` token sink. PB831 a silent wrong answer found while
re-measuring PB828. Appends where an owner already existed: PB499, PB550, PB389.

**The two corrections that matter more than the registrations.**

(1) **PB487's report said "a figurative constant has no intrinsic length"; the standard says otherwise.**
§8.3.3.1 defines a literal as *"a reserved word that references a figurative constant or … a character-string"*,
§8.3.3.6 *Figurative constant values* is a subclause of §8.3.3 *Literals* — which is exactly where SR9 sends the
reader for its length — and §8.3.3.6.4 GR3 supplies one: **b)** one character for a non-`ALL` figurative,
**c)** the length of literal-1 for `ALL literal-1`. Measured on the post-train-26 tree, `01 A VALUE SPACE.` now
yields `X(1)` — precisely GR3 b — while `01 G VALUE ALL "AB".` yields `X(1)` and stores `A` where GR3 c requires
`X(2)` and `AB`. That is PB831: a silent wrong answer on legal source, and its root is one sentence of
rationale in `DataBinder.PictureRequired.cs` that applies GR2 (a sized receiver) to the case GR3 governs. The
adjudication (PB828) therefore has an answer the tree is already half-implementing, and the FLAG-14 evidence
points the same way: §7.3.15.1 says FLAG-14 flags syntax whose behavior *"might be incompatible between the
previous COBOL standard and this"* — a compatibility flag is for syntax a compiler compiles.

(2) **PB487's sibling sweep enumerated the catch-alls by rule NAME and so missed one.** Measured: six
`genericClause` consumers over seven closed general formats — FD (§13.4.5.2) and SD (§13.4.6.2) through one
rule, the file control entry (§12.4.5.1), **I-O-CONTROL (§12.4.6.2, the inline alternative the sweep skipped
because it has no `xxxClause` wrapper)**, SPECIAL-NAMES (§12.3.7.2), the configuration-section paragraph list
(§12.3.2) and the IDENTIFICATION paragraph list (§11.2.1). All seven swallow `WIBBLE WOBBLE` at strict
`--std 2023` with no diagnostic (§4.2.2 requires a warning mechanism for general-format violations). And
SOURCE-COMPUTER is not one of them at all: it is `computerAttributes`, a `~DOT` token sink kept for the DELETED
'85 MEMORY SIZE / SEGMENT-LIMIT clauses and never edition-gated, which is PB830 — whose two rows,
`FMT-12.3.5.2` and `FMT-12.3.6.2`, read **CONFORMS** today on a witness (kb/Work PB78's optional computer-name)
that answers a different question. Two OK rows invisible to `DefectiveRowCoverage` while the format they claim
admits arbitrary words. The verdicts are deliberately NOT flipped here — a FMT re-verdict needs the per-format
audit the note prescribes — and PB829 records that PB487's own landing is the TEMPLATE for the other six:
`unrecognizedDataClause`, an error production that recognizes the word run and refuses it BY NAME at bind.

Everything was measured twice, on both sides of train 26 (`094739b3`, then `35f86874` after the rebase), and
every note carries which. That second pass paid for itself: PB499's "the map lives in CodeGen and must be lifted
to the model side" recommendation had been IMPLEMENTED by train 26 (`DataItem.TableValuePlan` / `ValueAt`), so
the note now says the obstacle is gone and the item is one change, not two; PB826's caveat about
`IntermediateExtremes` not being on main resolved; PB550's code-site line moved for the third time. PB550's two
quotations, which its own ⛔ REGISTRAR marker recorded as never validated, are now quoted verbatim with passing
`--check` lines.

Gate at the rebased head: `SpecTraceabilityInventory|DefectiveRowCoverage|DerivedVerdict` → **47 passed, 0
failed**; `audit_doc_citations.py --check` and `audit_code_citations.py --check` both at zero findings;
`work.py check` → 864 items, all well-formed; `gen_conformance_notes.py` → 4348 items · 2609 GAP, unchanged
(no verdict change, GAP delta 0).

