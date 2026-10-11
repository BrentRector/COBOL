## Entry 872 — 2026-07-17 10:07 PDT — P11 Step 1 — IntrinsicBind.Unsupported: the A.4.9 documented-non-support disposition exists as a CATALOG state; the five locale-module rows flip out of Deferred

The Deferred backlog's first structural move: `Deferred` ("will be implemented") and "the containing optional
module is documented non-support" are DIFFERENT verdicts, and P11's "zero Deferred" exit is only honest if
the five A.4.9 locale rows move to a state that is permanent by design, not a TODO. Per the scout notes
(`PHASE-11-scout-notes.md`, `spec:locale`):

- **`IntrinsicBind.Unsupported` added** (`IntrinsicCatalog.cs`) — XML-doc'd on §4.2.7 + A.4.1 (an
  implementation accepts an optional element's syntax ONLY when support is claimed; documented non-support is
  conforming) + A.4.9 (the locale module; ratified decision 3). The renderer's empty-`RuntimeMethod` loud
  fallback remains the backstop until the Step-8 COBOLNET1518 bind arm makes it unreachable.
- **Five rows flipped `Deferred`→`Unsupported`:** LOCALE-COMPARE §15.51 (A.4.9 item 2) · LOCALE-DATE §15.52
  (item 3) · LOCALE-TIME §15.53 (item 4) · LOCALE-TIME-FROM-SECONDS §15.54 (item 5) · STANDARD-COMPARE
  §15.85 (item 11). The scout's key nuance is now IN the catalog comment: STANDARD-COMPARE is NOT
  semantically locale-dependent — it consumes an ISO/IEC 14651:2020 cultural ordering table via SPECIAL-NAMES
  ORDER TABLE (§12.3.7 GR17) — and its independent non-support route is **A.3 item 25** ("the implementor
  need not accept the syntax" absent a 14651 implementation); the Step-8 diagnostic will cite BOTH.
- `SIG.Bind` has exactly four consumers (binder `== Fold` ×2, renderer `== Deferred || RuntimeMethod empty`
  ×2) — no exhaustive switch, so the new case is additive; an `Unsupported` reference today behaves exactly
  as yesterday's `Deferred` (same loud guard), i.e. this commit is behavior-neutral by construction.

Live `Deferred` set: **17 → 12 rows** (boolean pair, BYTE-LENGTH, the date-windowing trio,
SECONDS-PAST-MIDNIGHT, the four TEST-* validators, CONCATENATE). Battery on the staged tree: conformance
**3467/3467** · greenfield unit **292/292** · legacy 1196+636 ALL GREEN · guard-fast first pass **351+2**
(SQ223A COMPILE FAILED, IC234A DIFF) → **solo rerun 353 MATCH, 0 regressions, ALL GREEN** — the P10-close
IF137A-class environmental flake, re-proven; NEW data point: this occurrence had NO concurrent suite (the
guard ran alone, JOBS=32), so the flake class is internal binary contention, not only cross-suite.

