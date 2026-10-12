## Entry 128 — 2026-03-20: Grammar — SEARCH VARYING, VALUE THRU/THROUGH, ASCENDING KEY

Three grammar changes:
1. **SEARCH VARYING**: `SEARCH table (VARYING identifier)?` — NC232A, NC234A, NC236A now compile
2. **VALUE THRU/THROUGH**: Added THROUGH as synonym for THRU in valueItem, plus `literal+`
   alternative for multiple discrete values
3. **ASCENDING/DESCENDING KEY in OCCURS**: `occursKeyClause*` before `INDEXED BY` —
   NC233A, NC237A, NC238A, NC247A partially unblocked (some still have INDEXED BY issues)

Results: NC236A 5/5 (100%). NC232A and NC234A compile but have 3 SEARCH failures each
(index exceeds table size → AT END not triggered). NC201A/NC250A/NC252A still blocked
by other parse issues beyond VALUE THRU.

---

