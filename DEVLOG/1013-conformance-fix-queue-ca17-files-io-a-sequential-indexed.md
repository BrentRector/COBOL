## Entry 1013 — 2026-07-23 09:44 PDT — Conformance fix-queue: CA17 (files-io) — a sequential indexed REWRITE's prime-key change-detection is COLLATING-SEQUENCE-based, not ordinal (ISO §14.9.35 GR22 / §12.4.5.12.4 GR1)

**CA17 (CONFORMANCE-FIX-QUEUE, NIT/S, files-io) landed** — the first of the phase-14 independent-minors batch (an 8-item
pass separated out from the EC-infra/OO super-batch; the four EC/OO-entangled minors — CA12, CA30, CA37, CA38 — stay
with that coordinated pass). ISO §14.9.35 GR22: a sequential-access indexed REWRITE requires the replaced record's prime
key to be EQUAL to the last record read on the connector, else I-O status '21'. That "equal" is fixed by §12.4.5.12.4
GR1 (and §14.9.51 GR42) — record-key equality is **based on the collating sequence for the file according to the rules
for a relation condition**, NOT a byte compare.

**The bug.** `IndexedConnector.Rewrite` (:373) alone used the C# ordinal `if (prime != _lastReadPrime)` for the '21'
change-detection, while every other prime-key comparison in the connector — WRITE uniqueness, the REWRITE target lookup
(:375), DELETE — routes through the weight-aware `KeyEq`/`KeyCompare`, which select `_primeWeights` (resolved from a
§12.4.5.7 COLLATING SEQUENCE clause and wired end-to-end via `KeyedIoEmitter.WeightsLit(file.PrimeKeyWeights)`). So under
a prime-key collating sequence that weighs distinct characters equally, a legal same-key REWRITE (read `A123`, rewrite
`a123`) wrongly returned '21' and failed.

**The fix** (one line): `if (_lastReadPrime is not { } lastPrime || !KeyEq(prime, lastPrime, -1)) return Status =
FileStatusCode.SequenceError;`. `KeyEq(…, -1)` delegates to `KeyCompare` with `_primeWeights`, so the '21' detection now
uses the prime-key collating sequence exactly like WRITE (GR42) and the uniqueness checks (GR35), eliminating the lone
ordinal outlier.

**Blast radius = the collating case only.** With no prime-key COLLATING SEQUENCE the weights are null and `KeyCompare`
falls to `string.CompareOrdinal`, so `!KeyEq(prime, lastPrime, -1)` is byte-for-byte the old `prime != _lastReadPrime`
— identical for the entire non-collating corpus. The site is gated on `_access == KeyedAccess.Sequential`, so
DYNAMIC/RANDOM indexed files never reach it. **Golden** `tests/conformance/2002/ca17_indexed_rewrite_collating` —
ALPHABET ALPHA (`"A" ALSO "a"`), COLLATING SEQUENCE IS ALPHA on a sequential indexed file; read `A123`, REWRITE with
`a123` ⇒ collationally equal ⇒ FS='00' and the record is replaced (readback `K=a123 D=UPDATED`). Pre-fix produced '21'
and no replacement. **Gate (wave-local):** CA17 golden compile-strict+run+byte-match at --std 2002; manifest-integrity
5/5; files-IO/indexed golden sweep 19/19 green across editions; Debug solution build clean. Re-scout `wf_a09670d5-cdc`
confirmed the finding still valid with zero anchor drift.

