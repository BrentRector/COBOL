## Entry 914 — 2026-07-19 14:11 PDT — Review-fix: the COBOLNET1518 collision RESOLVED (method-REDEFINES → 1577) — the third of the class

Batch-3 verdict V11 (confirmed both lenses): 1518 was live-emitted with two unrelated meanings — the A.4.9
locale-module non-support (IntrinsicBinder, P11, documented in CONFORMANCE.md item 25) and the M2-OO
method-REDEFINES scope error (DataBinder, DEVLOG 639; PHASE4_RECONCILIATION.md:1793 recorded it as last-used
there, then the P11 plan double-allocated it). Same root cause as 1573: a bare compiler literal invisible to
the catalog — the `Edition.Error("COBOLNETnnnn"…)` dot-prefixed channel the Entry-907 drift guard does NOT yet
scan (this collision validates extending it; routed in the ledger). Fix: the method-REDEFINES error renumbered
→ **COBOLNET1577** via a catalog descriptor (`MethodRedefinesScope`, the descriptor overload — never a
literal); the two OoSpineTests pins updated in the same commit; 1518 stays solely = locale non-support (its
golden untouched, LocaleDispositionTests 11/11 green). PHASE4_RECONCILIATION note corrected; resume-prompt
band: 1577 taken, **NEXT FREE = 1578**. Gate: drift tests 5/5 · OoSpine 85/85 · Locale 11/11 ·
characterization 33 byte-exact · DIAGNOSTICS.md regenerated (+1 row).

