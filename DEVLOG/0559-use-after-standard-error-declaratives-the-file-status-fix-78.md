## Entry 559 — 2026-06-10 18:56 PDT — USE AFTER STANDARD ERROR DECLARATIVES + the FILE STATUS fix → 78 programs byte-match in one wave (776 conformance)

**The declaratives subsystem, implemented from the scout's decision-complete brief** (`use-declaratives-brief.md`)
**— the largest single-wave unlock yet: 61 SQ + 17 RL/IX programs locked.**

**Fix #0 first (the brief's prerequisite):** the ~30-program "hostpolicy RUNERR" sweep cluster was a DETERMINISTIC
backend CS0029 — `EmitStoreFileStatus` stored the status string into a GROUP-typed FILE STATUS item (the CCVS
`01 SQ-FS2-STATUS. 03 KEY-1 PIC X. 03 KEY-2 PIC X.` shape); it now stores through the image facility
(`FromImage`, §14.9.25.4 GR4) with mixed-usage groups loud, and an unresolvable DECLARED status name is LOUD
instead of silently skipped (§1.4 doctrine). Companion: a QUALIFIED FILE STATUS name (`SQ-FS4-STATUS OF
STATUS-GROUP`, SQ133A) captured its glued GetText — now the base word (the RENAMES capture pattern). ⚠ Sweep
lesson recorded: that cluster's "hostpolicy.dll" label was the runner tripping over a 0-byte dll + missing
runtimeconfig from a FAILED backend compile — never trust that label without checking for the runtimeconfig.

**The subsystem (ISO §14.2.4 / §14.9.49, per the brief's design):** declarative sections join the ONE pc space
(binder walks `declarativePart` BEFORE the body; `BoundProgram.EntryPc` = the first nondeclarative pc per
§14.2.3 GR1; SR4's PERFORM-into-a-declarative costs nothing — the UD7 fact). Each section's USE sentence binds
to a `BoundDeclarative` scope (SR1/SR2/SR7/SR8 diagnostics COBOLNET0897, BEFORE REPORTING staged-loud 0898);
leading handler sentences become an anonymous paragraph (the SQ103A shape). The emitter (only when declaratives
exist — declarative-free generated source is byte-identical) produces `__RunUse` (GR2 re-entrancy-guarded
bounded `__Dispatch(start, handlerEnd)`, try/finally so STOP RUN unwinds clean) and `__IoCheck` (the §9.1.13.1
gate: successful '0x' never fires; the statement's own AT END/'1x' or INVALID KEY/'2x' phrase suppresses its
family; file-scoped USE beats mode-scoped GR3/GR5; mode scope reads `CobolFile.OpenModeOf` — the connectors
record the ATTEMPTED mode of a failed OPEN ["being opened", GR6b] and reset to none on successful CLOSE
[§9.1.4]). Hooks after EVERY FILE STATUS store: sequential OPEN/CLOSE/WRITE/READ/REWRITE, all six keyed verbs
(per-statement phrase flags; DELETE FILE's ON EXCEPTION suppresses entirely), SORT/MERGE implicit I/O (GR12b —
the implicit READ is as-if-AT-END, so at-end never fires a declarative; implicit OPEN/WRITE/CLOSE failures do).
Sequential READ also gained the latent §14.9.30 GR24c/d fix: the AT END imperative now runs ONLY for the '1x'
family. The CCVS termination-tail accommodation (SQ212A's golden): a trivial exit paragraph followed by a
STOP-RUN tail inside the section caps `HandlerEndPc` there — bind-time, documented in COBOLNET_DESIGN §14.5.
Carrier change: `_paras` holds each paragraph's SENTENCES (declarativeParagraph and paragraphDefinition are
different context types); the ALTER scan re-keys on SentenceContext.

**Found while verifying: IX111A EXCEEDS the golden** — the failed indexed OPEN (35) fires the file-scoped USE
per GR3a, so OPN-TEST-GF-01-0 PASSES (001 OF 001); the golden encodes the legacy run where the indexed-OPEN
declarative never fired (000 OF 000 — a legacy-oracle hole; the sequential shapes agree with us, proven by the
7 differential facts). IX111A moved to swept-only/spec-pinned; re-baselines at G8.

**Verification: 776 conformance (7 new UseDeclarative facts + 78 new locks − IX111A) + 15 unit ALL GREEN; the
361-program census (SQ now included permanently) shows ZERO lost.** Residue queued: SQ212A needs variable-length
sequential WRITE/REWRITE → status 44 (the same fix the legacy needed, DEVLOG 311); ST144A DIFF 148 unexamined;
SQ DIFFs (17) + RUNERRs (5) untriaged — next session's first sweep.

