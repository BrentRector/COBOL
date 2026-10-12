## Entry 907 — 2026-07-19 12:25 PDT — Review remediation pt1 — the COBOLNET1573 collision RESOLVED (→1576) + the catalog-completeness drift guard

The review's one CRITICAL finding, fixed at the root:
- **The collision:** `RefModZeroLengthDirectiveProcessor` emitted a BARE `"COBOLNET1573"` literal (Entry 897)
  invisible to the catalog; Wave E's catalog-only next-free scan re-allocated 1573 to
  `external-file-status-consistency` (Entry 905). One user-visible code, two meanings.
- **The fix:** the RefMod malformed-operand diagnostic renumbered → **COBOLNET1576**, registered as a REAL
  catalog descriptor (`RefModZeroLengthMalformedOperand`), the frontend now emits via `.Code` — allocation is
  catalog-visible. 1573 keeps its single Wave E meaning (its golden pins it; no test pinned the RefMod 1573 —
  the cheap side renumbered). New negative `ref-mod-zero-length-malformed` pins 1576. DIAGNOSTICS.md regenerated.
- **The recurrence guards (2 new drift tests):** `EveryEmittedCode_IsACatalogDescriptor` (any
  `ReportError/Warning("COBOLNETnnnn")`-style emit in src whose code lacks a descriptor FAILS — it immediately
  caught 4 more bare frontend codes: 0718/0719 TURN-malformed/SR4, 0875 TURN-below-2002, 0883 PROPAGATE, all
  now registered with faithful descriptors, +5 DIAGNOSTICS.md rows) and
  `EveryDescriptor_HasUniqueCode_OutsideSplitFamilies` (code-level uniqueness — Id-uniqueness alone cannot see
  a two-names-one-code collision; the deliberate shared families 0899/1533/1535 exempted per the catalog's own
  notes). ⚠ KNOWN SCOPE LIMIT (routed to the review ledger): the compiler's `Edition.Error("COBOLNETnnnn"…)`
  dot-prefixed channel is NOT yet scanned — registering that channel's backlog is a named follow-on, not a
  silent gap. ALSO fixed while in the file: my first draft asserted `d.Id` where the COBOLNET code is `d.Code`
  (Id is the kebab name) — the test was self-diagnosing until corrected.

