## Entry 888 — 2026-07-17 22:35 PDT — PHASE-13 Wave C — WRITE … BEFORE AND AFTER ADVANCING (§14.9.51 SR17); the shared-parser lesson

Landed the COBOL-2023 combined **WRITE … BEFORE ADVANCING n AFTER ADVANCING m** (§14.9.51 SR17/GR25e/GR25f): the
grammar `writeBeforeAfter` was refactored to a repeatable `writeAdvancePhrase writeAdvancePhrase?`; the binder builds
a (before, after) pair with SR17 rejection (COBOLNET0862 for a malformed pair or PAGE in the combined form); a new
`BoundWrite.AfterAdvancing` field carries the AFTER phrase; the emitter routes the combined case to
`CobolFile.WriteBeforeAndAfter` → `SequentialConnector.WriteBeforeAndAfter`, which presents the line at the current
position then advances by BOTH amounts (both after presentation — GR25f relocates AFTER's advance) with
LINAGE-COUNTER += before+after. Recognition gate `WriteBeforeAndAfterAdvancing2023` fires on the co-occurrence (two
phrases) — a single BEFORE/AFTER stays edition-invariant. Golden `write_before_and_after` proves it via LINAGE-COUNTER
(OPEN=001 → BEFORE 1 + AFTER 2 → 004; a single BEFORE 1 alone gives 002, a single AFTER 2 gives 003, so 004 uniquely
proves both phrases parsed and applied).

**THE LESSON (corrects an earlier session assumption): the generated ANTLR parser is SHARED between the greenfield
and the frozen legacy compiler** — `src/CobolSharp.Compiler` binds against `CobolParserCore` too. Batch-1's grammar
changes were purely ADDITIVE, so they didn't break the legacy; but this `writeBeforeAfter` RESTRUCTURE broke the legacy
`FileIoBinder`'s `writeBeforeAfter().PAGE()/.integerLiteral()/.dataReference()` accessors. Fixed by pointing the legacy
binder at `writeBeforeAfter().writeAdvancePhrase(0)` (it consumes only the single/first phrase — a frozen oracle needs
no 2023 combined form). This is exactly why the standing rule requires a FULL legacy guard for every `.g4` change; the
greenfield-only claim in DEVLOG 887 was too strong — the `.g4` FILES live only in the greenfield, but their generated
output is shared. Also: the scout's golden used file-name `PF`, which is a RESERVED word (Report Writer page-footing) —
switched to `PRTF`.

`write_before_and_after` added to `GreenfieldOnly` (the legacy compiler has no combined-advance path). Battery:
greenfield conformance + unit 311 + characterization 33 + full legacy guard (NIST 353 MATCH) — all pending at commit.

