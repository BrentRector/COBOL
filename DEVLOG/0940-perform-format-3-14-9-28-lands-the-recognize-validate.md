## Entry 940 — 2026-07-20 20:53 PDT — PERFORM Format 3 (§14.9.28) LANDS the recognize/validate/diagnose/gate surface; runtime interceptor STAGED

Grammar-batch **7 of 7**. The exception-checking (Format-3) PERFORM (ISO §14.9.28 Format 3, COBOL-2023 — VCR row
79) is now a first-class RECOGNIZED, BOUND, and fully SYNTAX-CHECKED construct; the WHEN/OTHER/COMMON runtime
handler dispatch (GR17–20) is a documented next-wave GAP (COBOLNET0899). Implemented from the design SSOT
`docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md` after an 8-agent parallel anchor/convention
scout — which surfaced several places the design UNDER-SPECIFIED reality, each reconciled from the spec (not
improvised):

**What landed (full front half).** New reserved tokens `LOCATION`/`FINALLY`; the Formats-2/3 grammar MERGE into one
inline `performStatement` alternative + the `whenOperandAhead()` greedy-safe continuation predicate; `resumeStatement`
`AT?` fix (§14.9.33.2); `constructs.json` rows (`perform-exception-checking-2023` gate + `user-word-location-2023`);
`BoundExceptionPerform` bound node + its exhaustive-visitor arms; the `EcBinder.ExceptionPerform` binder — WHEN
operand resolution (all-level EC-names per the USE GR3a–3g tiers, SR16 EC-I-O, edition windows), the §14.9.28.3
syntax rules + cross-statement bans by lexical region (COBOLNET1597/1599/1600/1601/1604/1605/1606/1607/1608/1610/1611/
1612/1614/1615/1616/1617), the RESUME-in-WHEN relaxation (`EcBindState.InF3When` → RESUME NEXT legal, RESUME AT proc
= 1610), and the GR14 bind-time `TurnState.WithImplicitEnable` overlay over imp-1; the COBOLNET0900 introduction gate
(`VersionConformancePass.VisitPerformStatement`). Tests: 36 parse (Unit), the negative/gate/staged/behaviour/region
conformance suite (`PerformFormat3Tests`), the `WhenOperandStopTokens` completeness drift guard, and the
version-matrix rows.

**Reconciliations (design corrections, spec-grounded — all recorded in the design SSOT + D12):**
1. **FINALLY is NOT a `cobolWord`** (the scout's registry agent flagged the design's §2.2 exclusion vs its §7
   continuity claim). As a trailing phrase keyword after imperative statements, a FINALLY admitted to the name-slot
   funnel is SWALLOWED by a preceding DISPLAY/MOVE operand list (`DISPLAY "c" FINALLY` → FINALLY becomes a DISPLAY
   operand, the FINALLY phrase vanishes — caught by a parse test). Resolution: FINALLY is a pure reserved keyword at
   every edition (a documented, negligible deviation — FINALLY was never a COBOL identifier idiom; zero corpus hits).
   **LOCATION stays a `cobolWord`** — it appears only in the head (never after a statement), so no swallow, and the
   continuity invariant (`PERFORM LOCATION` / a paragraph named LOCATION below 2023) needs it. Asymmetric, justified.
2. **The merged inline arm must precede the out-of-line `PERFORM procedureName`** — `PERFORM LOCATION imp… END-PERFORM`
   is genuinely ambiguous (LOCATION is a cobolWord ⇒ a valid target), and only the trailing END-PERFORM disambiguates;
   ANTLR picks the first VIABLE alternative, so inline goes first (period-terminated `PERFORM LOCATION.` falls through).
3. **COBOLNET1598 (operand-form exclusivity) NOT emitted** — no §14.9.28.3 SR backs it; SR14/15/16 are the only WHEN
   operand rules and the figure's `{exception-name-1 | exception-name-2 FILE file-name-2}…` permits interleaving.
   Spec-fidelity: implement the NAMED rules, invent no restriction. XS-RESUME-PLACEMENT subsumed by COBOLNET0712.
4. **`IsLexicallyWithin` / `>>POP`/`>>PUSH` do not exist in the greenfield** (scout binder agent). Region bans are
   parse-subtree walks (regions A–D fall out of the F3 node's own sub-lists — simpler than the design implied).
   XS-POP/XS-PUSH (would be 1602/1603) stay RESERVED — the directives are themselves unimplemented, so the ban rides
   that wave. **TurnState is immutable** → GR14 is a `WithImplicitEnable` derived-instance overlay (not push/pop).
5. **The diagnostic band is compiler-channel raw codes** (the 1585–1596 pattern) — NOT DiagnosticCatalog descriptors
   (both the src grep [max 1596] and the catalog [max 1584] confirmed; the design's stale "lands in DiagnosticCatalog"
   line discarded).

**Why the runtime is STAGED (the LANDABLE-vs-STAGED boundary, drawn honestly).** Implementing the interceptor
surfaced a real defect in the design's lambda sketch: **C# cannot `goto` out of a lambda**, so an `EXIT PERFORM` inside
a WHEN handler (imp-2, emitted in the matcher closure) cannot jump to the PERFORM's end label; plus threading `fatal`
through the `EcDispatchExpr` seam touches many raise sites and the lambda-mode statement emission is subtle. A
half-correct interceptor risks SILENT MISCOMPILES and could destabilise the differential oracle. Per the design's own
§5.4 framework and the batch-2 precedent (PICTURE EDITING staged its render; VALUE Format 2 staged multi-dim — each a
0899 REJECTION, not a silent partial compile), the boundary is drawn to land the complete diagnose/gate surface and
**reject** the F3 PERFORM at 2023 with a loud COBOLNET0899 (the `perform-exception-checking-2023` construct row is
`status:pending`). Rejection (vs compiling imp-1+FINALLY with a warning) is the safe, honest, batch-2-consistent
choice — it avoids a silent on-raise divergence (the WHEN not firing while the program still runs). NEXT WAVE = the
ambient F3-frame stack + `__EcPerform` per-statement interceptor (its blocker: a non-lambda handler-emission strategy,
since C# forbids `goto` out of a lambda).

**Adversarial review (5 parallel skeptics, each spec-grounded) caught real defects — fixed in the same change set.**
(1) **SR15 (COBOLNET1600) false negative** — a BARE exception-name coexisting with a FILE-paired instance of the
SAME name slipped through (`files.Distinct()` treated `null` as a licensing distinct file-name); fixed to reject any
multi-occurrence group containing a bare instance (§14.9.28.3 SR15). (2) **GR14 overlay bug** — the implicit-enable
synthetic was stamped at line 0, so a real `>>TURN OFF` *before* the PERFORM defeated it (the opposite of GR14);
re-implemented as a line-sorted splice at imp-1's line with a "not-already-enabled" guard (GR14/GR22). Latent (the
overlay's output is unused while the runtime is staged) but a genuine landed bug. (3) **IsFormat3 discriminator
de-duplicated** (binder ↔ 0900 gate) to a shared `internal static` — the 0899↔0900 hand-off can no longer drift
(DEVLOG-724 class). (4) Added the missing negative tests the review ranked (1607/1617/0712, region-C breadth, the
mode-list file-name continuation parse test, the SR15 regression) + a `NotEmpty` guard so the stop-set drift test
cannot pass vacuously. Documented, not fixed (masked while staged): the `inlineMethodInvocationStatement` (the one
cobolWord-led statement) can still be annexed as a bare imp-2 — the interceptor wave adds an LA(2)==LPAREN gate; and
1613 (multi-file DELETE FILE) is unreachable under the single-file `deleteFileStatement` grammar (reserved like
1602/1603). Conformance F3 tests 21→27; parse/drift Unit tests grew accordingly.

**Process note (transparency).** The shared `.g4` change broke the LEGACY `CobolSharp.Compiler` oracle
(`performOptions` moved under `performInlineHead` ⇒ its accessor went array→single) — fixed the two legacy
`ControlFlowBinder` sites in the same change set (the shared-grammar-⇒-full-legacy-guard rule).

