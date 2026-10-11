## Entry 948 — 2026-07-21 13:52 PDT — Wave D (cont.): the FLAG-02/FLAG-14 flagging DESIGN SSOT — 8-agent spec+code scout + adversarial census verify → decision-complete deep-dive

**The task.** The Wave-D remainder's headline item: turn the recognized-but-ignored `>>FLAG-02` (§7.3.14) /
`>>FLAG-14` (§7.3.15) directives into REAL migration flagging (GR1: the implementor *shall* provide a warning
mechanism). Per the owner's "careful shared-core, do NOT rush, each direction recorded" directive + the
persist-anchor-rescout rule, I scouted before writing code.

**The scout (Workflow, 6 agents + 2 adversarial verifiers, 510k subagent tokens).** Four parallel spec+code
scouts: FLAG-14 (all 13 GR4 sub-rules), FLAG-02 (all 6), the code-infra scout (directive intake / warning
emission / detection hooks / VCR ledger / recommended framework), and a direction scout (>>COBOL-WORDS +
CC-in-COPY + copybook >>SOURCE FORMAT). Then two adversarial verifiers re-derived every GR4 citation, E.2
anchor, and spot-checked code anchor. **The verify caught one real completeness defect:** the FLAG-14 census
dropped option **m) WRITE-END-OF-PAGE** (GR4 enumerates a–m = ALL + 12; the first pass stopped at l) and
miscounted "twelve vs thirteen"; both fixed. FLAG-02: all 6 CONFIRMED, zero defects. I ran my own independent
recon in parallel and confirmed all four infra anchors: `VersionConformancePass` (the two-arm walker already
matches READ PREVIOUS/MOVE/CLOSE/SET/VALUE for edition gating), the `TurnEvents`/`RefModZeroLengthEvents`
line-anchored toggle-stream template (REF-MOD-ZERO-LENGTH is itself BOTH a standalone directive AND FLAG-14
option i), `EditionSeverity.Warning` catalog descriptors, and next-free diag = 1620.

**Structural decision (mine, spec-first, singular-pattern).** A **dedicated `FlagConformancePass`** sibling to
VersionConformancePass — NOT a bolt-on: flagging is an orthogonal axis (user directive-state, always Warning,
fires regardless of `--std`), and two of its detectors (compile-time-arithmetic b, `>>EVALUATE` c) have no
bound-tree residue at all. It reuses the ONE drift-proof `StatementChildren()` traversal (no second bespoke
switch-walk — `feedback_path_a_leverage_tooling`). ONE `FlagState` per-option toggle-fold (the
RefModZeroLengthState template), ONE `FlagDirectiveLine` parser + `FlagOption` catalog, TWO collection sites
forced by construct-visibility (bound options post-COPY via a new `FlagDirectiveProcessor`; the frontend-only
b/c inline in `ConditionalCompilationProcessor` from an in-scan `FlagScanState`). Diagnostics **COBOLNET1620**
(Flag02Warning) / **1621** (Flag14Warning), two codes carrying per-option ConstructId/Message/Citation.
Directive-word edition gates: `>>FLAG-14` = 2023 introduction (0900 below), `>>FLAG-02` = 2014 intro / 2023
obsolete (0903). Copybook-internal `>>FLAG` rides the CC-in-COPY fix.

**Deliverables (this commit — design-first, no code yet).** `docs/rearchitecture/DESIGN-flag-directives.md`
(decision-complete: the verified per-option census mapped to GR4 + Annex E.2 + detection sites, the 6
structural decisions D1–D6, the three seams, and a 5-increment plan — Incr 0 core+READ-PREVIOUS through Incr 4
the new-analysis options I-O-DECLARATIVE / I-O-STATUS-04/07 / EC-PROGRAM-EXCEPTIONS). DOC_INDEX row added; plan
§0 NEXT block rewritten to the SSOT pointer + the recorded directions for >>COBOL-WORDS and CC-in-COPY. Green
baseline unchanged (docs-only). Implementation Incr 0 is next.

