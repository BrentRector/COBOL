## Entry 939 — 2026-07-20 19:34 PDT — PERFORM Format 3 (§14.9.28) design corrected + persisted; the rejected re-derivation deleted

Not code — a design + document-integrity pass, at owner direction ("resolve the open questions from the PDF, not
from me"; "remove the errors and replace with the correction; no known errors in the documents"). Three threads:

**(1) The three "open questions" were figure facts, resolved from the PDF — and were already in the repaired
markdown.** I had escalated them for owner adjudication (≥1 WHEN required? WHEN operand mixing? `IO` vs `I-O`?). That
was a process error: the standing rule is to render the PDF, never punt a figure-reading. Rendered page 712 at 400 dpi
(`render-spec-page.py`): the outer WHEN delimiter is a BRACE (≥1 WHEN required), the operand is three STACKED
alternatives (no mixing), and `IO` is a typesetting defect for `I-O` (an underlined format word must be §8.9-reserved;
`IO` is not, `I-O` is). Crucially the 2026-07-19 diagram-repair had ALREADY put these in the §14.9.28.2 markdown notes
— the render only CONFIRMED the repair held. Recorded in `PHASE-13-c5-perform-format3-pdf-resolution.md` and durably in
memory `feedback_spec_diagrams_render_pdf` (trust the repaired figure NOTES first; render to verify; never escalate a
figure-reading).

**(2) A corrected, adversarially-verified design SSOT** (`PHASE-13-c5-perform-format3-DESIGN.md`), produced by a
5-agent workflow (full SR enumeration · greedy-safe grammar derivation · GR14-22 runtime derivation · adversarial
verify · synthesis). It fixes every C5 verdict defect: the greedy `useEcEntry+` mis-parse (a `whenOperandAhead()`
continuation predicate — the exact cobolWord ∩ statement-leaders set, guarded by a build-time drift assertion — so a
WHEN operand can't swallow a following `RESUME`); the ~16 missed cross-statement SR bans (POP/PUSH, EXIT PERFORM
CYCLE, CLOSE/DELETE/OPEN/INITIALIZE/MERGE/SORT in imp-1, INITIATE/TERMINATE/VALIDATE, GO TO/RESUME/RAISE placement);
the RESUME `AT?` optional-word fix; and the GR14-22 exception-checking runtime with an explicit landable-vs-staged
boundary.

**(3) Document integrity — errors REMOVED, not marked.** Per owner directive the rejected re-derivation JSON was
DELETED (`git rm`), not annotated, and the correction put in its place; all references (§0 ×2, the resolution note,
the historical DEVLOG pointer) repointed to the design; the SR8 and FINALLY-abnormal "route to owner" notes corrected
to spec-resolved / recorded-standard-defect-with-a-chosen-default. THE caught error: the workflow's synthesis said
"next-free diagnostic 1585" from a catalog-ONLY scan — but batch 2's 1585-1596 are compiler-channel raw codes not in
`DiagnosticCatalog.cs`. Shifted the whole allocation to the true contiguous free block 1597-1617 (both scans agree);
also caught+fixed a leading-zero corruption my own shift script introduced (`COBOLNET0900`→`COBOLNET900`). The design
is now the ready SSOT for the PERFORM Format 3 implementation wave (starting next session after a context clear).

