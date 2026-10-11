## Entry 623 — 2026-07-06 01:40 PDT — Phase 4 track (d): the file-sharing / record-locking subsystem (M2-FILE-1) — LANDED

**The COBOL-2002 file-sharing / record-locking subsystem is live end-to-end** — the `SHARING` clause + `OPEN
SHARING` phrase (§12.4.5.15 / §14.9.27), the `LOCK MODE` clause (§12.4.5.9), the `RETRY` phrase (§14.7.9), the
`WITH LOCK` / `WITH NO LOCK` / `IGNORING LOCK` record-lock phrases (§14.9.30/.51/.35), and the `UNLOCK` statement
(§14.9.47), with the six sharing statuses **51/52/53/54/61/62** (§9.1.13.8/9) and the `COBOLNET1512` SR band. Built
FROM the decision-complete design in `docs/PHASE4_RECONCILIATION.md` §M2-FILE-1 (synthesis decision **D1**).

**D1 — built REAL, not stubbed.** §9.1.13.9 keys 61/62 on *"another file connector"* (not another run unit), and
two `SELECT`s bound to one resolved host path are two connectors over one physical file, opened concurrently in a
single-threaded run unit — a deterministic, testable conflict. So the machinery is a physical-file registry
(`src/Cobol.Net.Runtime/IO/CobolFile.Locks.cs`) keyed by host path: open connectors → Table-19 → **61**; record
locks (owner connector per record) → **51** for another connector's access; UNLOCK releases them; the RETRY loop
re-checks n+1 times. The golden `tests/conformance/2002/file_sharing.cob` proves it in ONE run unit — two RELATIVE
connectors on `share.dat`: `OPEN-A=00 / OPEN-B=00 / READA=00` (A locks RRN 1 WITH LOCK) `/ READB=51` (B blocked)
`/ RETRYB=51` (A can't release mid single-thread) `/ IGN=ALPHA` (IGNORING LOCK bypass) `/ AFTER=00` (post-UNLOCK)
`/ EXCL=61` (B reopens SHARING WITH NO OTHER while A open).

**AS-BUILT vs the design (deviations, each for a stated reason — the deep-dive is updated in the same change
set):** (1) **Default posture** — the design said "every file NoOther default"; the as-built makes a connector
"sharing-active" only once it declares a SHARING/LOCK MODE clause (or an OPEN carries the phrase), so a file with
NEITHER clause keeps the legacy exclusive path **byte-for-byte** — this is the *reason* the design cited for the
NoOther default (no-regression), achieved more conservatively (the whole pre-2002 corpus never touches the
registry; the legacy integration suite + NIST confirm zero drift). (2) **`Locked` HashSet kept** for the ≤2014
CLOSE…WITH LOCK/38 path rather than merged into `PhysicalFileState` (the design's "free 38 bug-fix" is a deferred
cleanup — retained to minimize risk). (3) **Record-lock runtime effect threaded on the keyed RANDOM read path**
(the golden's leg) — sequential-organization record locking has no per-record identity in this model and stays
SR-validated-only (documented residue; `CurrentRecordId` returns "" for sequential → locking suppressed). The SR
diagnostics (`COBOLNET1512`) fire for ALL organizations regardless.

**Deliverables (all in this change set).** Grammar (shared frontend, all additive `{is2002()}?`-gated, unique
leading tokens — the DEVLOG 621/622 lesson honored): 9 lexer tokens (SHARING/RETRY/UNLOCK/MANUAL/AUTOMATIC/
IGNORING/FOREVER/SECONDS/ONLY) + `sharingClause`/`lockModeClause`/`sharingPhrase`/`retryPhrase`/`recordLockPhrase`/
`readAdvancingOnLock`/`unlockStatement`. Binder: `FileModel` sharing/lock fields, `DataBinder` SELECT arms + SR2/SR8
checks, `BindOpen` SHARING/RETRY + SR8, `BindUnlock` + `CheckRecordLockPhrase` (SR4), the `BoundUnlock`/`RetrySpec`/
`BoundRecordLock` nodes (registered in `BoundStores` + the EC walk). Runtime: the `FileStatusCode` 51–62 band, the
`FileSharing`/`FileLockMode`/`FileRecordLock`/`FileRetryKind` enums, the `CobolFile.Locks` registry (Table-19
`Conflicts`, `LockRecord` with 53/54 ceilings, `ReadLockGovern`, `Unlock`, `RetryLoop`). Emitter: `RegisterSharing`,
`OpenShared`, `ReadLockGovern`, `EmitUnlock`. Diagnostics: `EditionGateHints` Sharing/LockMode/Retry/Unlock gates
(0900). Registry+matrix: 5 introduction rows + 1 user-word row (drift-checked both directions). Tests: the golden
(GreenfieldOnly in `ConformanceTests.cs` — the frozen legacy binder has no sharing support), 10 `CobolFileLockTests`,
4 SR negatives (SR8/SR4/SR2/SR1 → 1512). **GREEN: 201 unit + 1844 conformance + 557 legacy integration; full guard
running.** No 2023 removal here (pure 2002 introduction); the CLOSE…WITH LOCK 2023 removal was already gated.

