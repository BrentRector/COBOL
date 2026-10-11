## Entry 286 — Baseline ST chain consumers ST103A + ST105A (NIST 284→286)

Baselined two producer/consumer ST chains, verified by running producer→consumer from a clean dir:
- **ST104A → ST105A** (2/2): ST104A builds the sort input file TF001, ST105A verifies it.
- **ST101A → ST102A → ST103A** (9/9): ST101A builds TF002 (its SORT output), **ST102A** updates it, ST103A
  verifies. ST102A produces no CCVS report (it is a *builder*), so it is added to the guard as a NON-baselined
  producer — it runs (building/updating the file) and the guard reports it "NO BASELINE", which is not a
  failure. Guard comment documents the chain ordering (members consecutive, producers ahead of consumers).

Guard ALL GREEN: **286 NIST** (… + 40 IX + 16 ST), 0 regressions. The other chain consumers
(ST107A/114M/117A/121A) fail even with their assumed producer (wrong producer or genuine per-test bugs —
e.g. ST117A is 1 FAIL* after its ST116A builder), deferred for individual investigation.

