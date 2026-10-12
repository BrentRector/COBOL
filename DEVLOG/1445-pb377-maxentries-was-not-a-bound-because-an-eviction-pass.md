## Entry 1445 — 2026-09-03 10:36 PDT — PB377: MaxEntries was not a bound, because an eviction pass dropped every overflow raised while it ran

The Linux unit shard failed `CollationKeyCacheTests.ConcurrentCallers_GetOneKeyPerText_AndNeverAWrongOne` on three
of eight `main` runs on 2026-09-02/03 and passed on every re-run; Windows never reproduced it. The first thing the
CI log settles is WHICH assertion failed: line 151, `Assert.True(cache.Count <= 64)` — the SIZE BOUND, not the
wrong-key assertion beside it. No caller ever received a key built for a different text, on CI or in the ~2.6M
lookups the implementer stressed since, so the note's provisional `wrong_answer: true` was reverted at landing;
what failed is the cache's memory bound, and `severity` was raised to MAJOR for it (a configured 512-key cache
holding 1,594 keys is unbounded growth under concurrent SORT/MERGE or INDEXED-key load, not a flaky test).

`Evict` was a try-once batch: `CompareExchange(ref _evicting, 1, 0)` and, on failure, `return`. The winner took its
removal budget once, before it started, and every caller that overflowed while it held the flag went on and left no
trace of itself anywhere — no pending flag, no queue. So a pass ended with `keep + (however many racers arrived)`
entries, repaired only if some later insert happened to overflow while the flag was clear. When the workload ends
inside a pass — a `Parallel.For` whose eight workers all finish within one eviction window, precisely this test's
shape — no such insert ever comes. Measured on the pre-fix cache: 73 keys for a configured 64 (the CI number), 90 in
another round (the whole working set), a live peak of 213 for a cap of 64, and 1,594 for a cap of 512. Linux-only
in CI because the failure needs the eviction window to be long relative to an insert, and a 2-core runner executing
5,293 tests under xUnit's parallel collections deschedules the evicting thread mid-pass; 30 unpinned and 40 two-core-
pinned Windows runs of the shipped test stayed green.

The fix puts the invariant where it belongs — the thread that evicts owns the whole job, including the overflows
raised while it held the flag. `Evict` became a drain loop over an extracted `EvictOneBatch`, re-deriving the budget
from the live count each batch and re-checking after it releases the flag, with a no-progress bail so it cannot
spin. The release is `Interlocked.Exchange`, not `Volatile.Write`, and that is load-bearing: a racer's increment of
`_count` happens-before its failed CAS on `_evicting`; that failed RMW and the release are operations on the same
location and so are totally ordered; a full barrier then forbids the loop's re-read from being hoisted above it,
which a release-store/acquire-load pair does not. `MaxEntries` is documented for what it can be — a QUIESCENCE
bound: the instantaneous count still rides over it by design and settles at or under it once the last caller
returns. Two siblings of the "counter and map disagree" class went with it: `Clear` wrote a flat zero (discarding a
racing insert's increment — the unsafe direction, since the cache then never reaches its trigger) and now re-reads
from the map; `Lookup` decided whether to count an insert by asking whether the returned KEY was ours, sound only
while `Collator.GetKey` allocates per call, and now asks whether our ENTRY won.

`TheMaximum_IsABound_OnceTheCallersStop_HoweverTheyRaced` is the drift test: against the un-fixed cache it fails
("round 1: the cache settled at 536 keys, over its maximum of 512"); against the fixed one it passes in 243 ms, 0
violations in a 60-round soak, 0 failures in 25 two-core-pinned iterations. One intermediate "fixed" reading was a
false green from a stale dll an incremental build had not refreshed; it was caught by md5-summing the assemblies and
the whole A/B redone hash-verified. The implementer's sweep (`CompareExchange` across `src/`) surfaced one sibling
of a different shape — `CollationRuntime.Initialize` publishes its one-shot flag BEFORE the work, so a concurrent
loser returns and may read `DefaultConfig` before the winner assigned it from the environment; reachability
unmeasured — registered as `PB380`, not fixed here. Also noted and not a defect: `build-local.ps1` runs the Unit
project unfiltered by design (plan §0 "Gates"), so that leg is always the full 5,294.

Landed from the implementer's worktree branch (three WIP checkpoints, `5d946980`) by checking out its three files
onto main — its `STATUS.md` is gitignored and never entered the landing. Wave-local gate on main, filter
`~Collation|~RuntimeConfig|~Sort|~Merge|~Locale|~Indexed`: verdict lines in the commit message.

