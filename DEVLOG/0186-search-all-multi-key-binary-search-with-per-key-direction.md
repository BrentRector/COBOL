## Entry 186 — 2026-05-28: SEARCH ALL multi-key binary search with per-key direction — NC237A 100% (19 remaining)

**Bug (NC237A IDX-TEST-F2-9/12/13):** SEARCH ALL on a table with mixed-direction keys
(`ENTRY-310-2 ... ASCENDING KEY GRP-1 DESCENDING KEY SEC`). The WHEN tests
`GRP-1(...) = "05" AND SEC(...) = "07"`. All three returned the AT END (FAIL) path.

**Root cause** — the binary search used a single direction: `ExtractFirstRelationalComparison`
took only the *first* equality (GRP-1) and a table-wide `isAscending` flag. When several
rows share GRP-1="05" and are ordered by SEC *descending*, deciding the search half from
GRP-1 alone steps the wrong way and misses the row.

**Fix** — proper multi-key binary search. New `ExtractSearchKeys` walks the ANDed WHEN
equalities in priority order and matches each key by name to the table's
ASCENDING/DESCENDING key lists to recover its direction (`SearchKey` record). At each
node, `EmitSearchKeyDirection` compares keys in order and, at the first key that differs
from its target, branches into the lo/hi half by *that key's* direction
(asc: key<target→right; desc: key<target→left). Single-key tables are the degenerate
case (unchanged behavior); unclassifiable conditions fall back to the existing linear
scan. NC237A: 3→0 FAIL*, now 13/13. Guard green (all other SEARCH ALL tests — NC170-173A,
NC231-238A — still MATCH). `ExtractFirstRelationalComparison` removed (replaced by
`ExtractSearchKeys`); decomposition unit test updated.

---

