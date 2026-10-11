## Entry 993 — 2026-07-22 21:49 PDT — The ~44 candidates VERIFIED: 44 confirmed (fix-ready, with spec-derived goldens), 2 reclassified to owner-decision

Independently verified every audit-surfaced candidate (V54–V59 + CA1–CA38) against the spec text AND the code
(`wf_29a15db2`, 14 agents by area, each producing a decision-complete spec-derived fix + a spec-derived golden per
confirmed item). Result: **44 CONFIRMED (2 blocker, 30 major, 10 minor, 2 nit) · 0 refuted · 2 reclassified to
NEEDS-OWNER-DECISION.** SSOT `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` (the verified, fix-ready queue).

**Not rubber-stamping** — the pass earned trust: the CA5 agent FOUND a mirror bug during verification (multi-receiver
missed-PROHIBITED) and corrected the auditor's own `10/3*3` example (intermediate-precision-sensitive) with a
precision-independent golden; the CA4 agent cross-checked that MULTIPLY/DIVIDE correctly LACK the GIVING exclusion so
the fix is scoped to ADD/SUBTRACT; and it DOWNGRADED two from "bug" to owner-decision — **CA14** (SYNC-on-group: the
strict/default axis already rejects &lt;2023 correctly; only `--permissive` accepts-inert, an explicit documented owner
disposition, and ISO defines no `--permissive` — §4.2 permits standard extensions) and **V59** (REDEFINES Tier-B
BINARY/PACKED zoned image: §13.18.60.4 GR4/GR11 make the byte representation EXPLICITLY implementor-defined, the value
round-trips, and no CONFORMING program can detect the radix — so not a §4.2.16 violation; the real question is
GnuCOBOL/real-program byte-pun fidelity, an owner/architecture call).

**Blockers to fix first: CA31 + CA32** — EXIT PERFORM (without/with CYCLE) in a multi-level inline PERFORM VARYING
emits a bare `break;`/`continue;` which in the nested-`while` emission exits only the INNERMOST loop, so the whole
PERFORM is not left and extra AFTER-level iterations run (§14.9.14.4 GR5a/GR6). One shared fix: route EXIT PERFORM
through goto-to-labels bracketing the loop (reuse the F3Region machinery — add `F3Region.Inline`); regenerates the 32
characterization snapshots (a spec-correct re-baseline). NEXT SESSION: work `CONFORMANCE-FIX-QUEUE.md` top-down by
severity, spec-first, batched by area, each fix landing WITH its spec-derived golden under a comprehensive gate; then
Phase A of the full review (`DESIGN-spec-conformance-review.md`).

