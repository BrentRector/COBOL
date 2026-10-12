## Entry 887 — 2026-07-17 21:55 PDT — PHASE-13 Wave C (batch 1) — 7 COBOL-2023 grammar constructs, spec-first from a persisted re-scout

Ran the standing per-wave discipline: an 8-agent parallel spec-first anchor re-scout FIRST (persisted as
`docs/rearchitecture/PHASE-13-wave-c-scout.md`, TRUSTED over the drift-prone plan — [[feedback_persist_anchor_rescout]]),
then implemented one construct at a time, CLI-probing each + shipping its golden + a below-2023 negative in the same
batch. **The re-scout caught two audit-drift errors before any code:** (1) the dynamic-length SET form is
`SET [SIZE OF] data-name TO n` (§14.9.39 Format 16) — NOT the audit's "SET LENGTH OF"; and its EC is
EC-STORAGE-NOT-AVAIL, NOT the audit's EC-BOUND. (2) The boolean-shift precedence (rule 7b) is context-sensitive
(a shift inherits the precedence of its preceding operator), not a fixed grammar tier.

**Landed (each: superset grammar + binder + emit + runtime + `constructs.json` row + VersionConformancePass gate +
conformance golden + below-2023 negative):**
1. **GOBACK … WITH {ERROR|NORMAL} STATUS** (§14.9.18.2, VCR 75) — reused a shared `statusPhrase` rule (renamed from
   `stopStatusPhrase`, singular-pattern) referenced by both STOP and GOBACK; gated `GobackStatus2023` (0900).
   Presence-only, matching the STOP sibling — the status VALUE → exit-code wiring is a separate STOP+GOBACK
   termination-status slice (tracked). Golden `goback_status`.
2. **USAGE PACKED-DECIMAL WITH NO SIGN** (§13.18.60.4 GR11, VCR 42) — `PicInfo.PackedNoSign` drops the sign nibble
   (StorageWidth `ceil(Digits/2)` vs `Digits/2+1`); value path identical to unsigned packed. New rejects: NO SIGN on
   a non-Packed usage (COBOLNET1565), 'S' picture + NO SIGN (SR31, COBOLNET1566). Recognition gate
   `UsagePackedNoSign2023`. Golden proves the byte-width delta via `FUNCTION BYTE-LENGTH` (BL-PLAIN=4 vs BL-NOSIGN=3).
3. **63-character COBOL words** (§8.3.2.1, VCR 54) — a length CEILING (a relaxation, opposite direction from a 0900
   introduction): `VisitCobolWord` rejects >max (COBOLNET1567) where max = 63 (2023) / 31 (2002/2014) / 30 (1985);
   >63 rejected at EVERY edition. Deduped per distinct word.
4. **SET [SIZE OF] dyn-length TO n** (§14.9.39 Format 16, VCR 60) — new `setSizeStatement` + a bare-form peek
   (`SET dyn TO n`), `BoundSetSize`, runtime `CobolDynString.SetSize` (GR39 space-fills GROWN positions, never
   restores truncated content — the golden is deliberately shrink-then-grow 5→3→6 = `HEL   `). Semantic gate
   `SetDynLengthSize2023` on the bound node (covers both forms). SR33 reject COBOLNET1568. Golden `set_size`.
5. **CONTINUE AFTER n SECONDS** (§14.9.9, VCR 57) — `BoundContinueAfter` + runtime `CobolTiming.ContinueAfter`; a
   negative interval → 0 (GR1a) and, under `>>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON`, sets that nonfatal EC
   (GR1b, observed via `FUNCTION EXCEPTION-STATUS`); the check flag is captured from the TurnState at bind. m=0
   implementor choice (integer seconds). Gate `ContinueAfter2023`. Golden `continue_after`.
6. **PERFORM … UNTIL EXIT** (§14.9.28.4 GR11, VCR 80) — `PerformForever` control → `while(true)`; escape by
   inline EXIT PERFORM. Gate `PerformUntilExit2023`. Golden `perform_until_exit`. (The Format-3 exception-checking
   PERFORM — a large EC-entangled construct — is a documented follow-on, per the scout's split recommendation.)
7. **Boolean shift B-SHIFT-L/R/LC/RC** (§8.8.2 rule 8, VCR 9/32/46) — 4 lexer tokens + a `booleanShiftTerm` tier
   (integer 2nd operand, rule 5) + `BoundBoolShift` + runtime `CobolBool.Shift{Left,Right}[Circular]`. Output matches
   the Annex A Table A.2 oracle byte-for-byte (1100 → SL3=0000/SR3=0001/SLC=0110/SRC=1001). Recognition gate
   `BooleanShiftOperators2023` (a `HasShiftOp` scan parallel to `HasBoolOp`) — correctly distinct from
   boolean-operators-2002 at `--std 2002`. **The rule-7b context-sensitive precedence (a shift inheriting a preceding
   B-OR/B-XOR's precedence) is a DOCUMENTED refinement** — the fixed tier realizes the unmixed default case (the
   oracle + all realistic usage); mixed shift+binary precedence is staged. `BooleanRenderer` was refactored to thread
   a `NumericRenderer` (the shift count is a numeric operand).

**Diagnostic band:** consumed COBOLNET1565–1568 (NO SIGN ×2, word-length, SET-SIZE SR33); next free 1569. Six new
`constructs.json` introduction gates use 0900; the word-length ceiling uses 1567.

**Battery:** greenfield conformance green (features 1–4 verified at 3602; 5–7 + boolean-shift run pending at commit),
unit 311, characterization 33 (the `BooleanRenderer`/`ConditionRenderer` refactor byte-neutral), full legacy guard
(NIST) pending — the grammar is greenfield-only (`src/Cobol.Net.Frontend/Grammar`), so the legacy CLI/NIST is
structurally unaffected, run as the standing backstop. **Remaining Wave C:** WRITE BEFORE AND AFTER + SUPPRESS WHEN
(C5), PICTURE EDITING (C3), PERFORM Format 3; then Waves D–I. Persisted anchors: `PHASE-13-wave-c-scout.md`.

