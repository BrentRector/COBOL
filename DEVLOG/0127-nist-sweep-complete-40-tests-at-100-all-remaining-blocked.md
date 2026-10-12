## Entry 127 — 2026-03-20: NIST Sweep Complete — 40 Tests at 100%, All Remaining Blocked

### Final sweep status

Exhaustive compilation of all 93 NIST kernel programs. 40 tests at 100% (including NC121M).
All remaining tests are blocked by grammar-level issues that require grammar changes:

| Category | Tests | Required Change |
|----------|-------|-----------------|
| SEARCH VARYING | NC231A, NC232A, NC234A, NC236A | Add VARYING clause to searchStatement |
| ASCENDING KEY | NC233A, NC237A, NC238A, NC247A | Add KEY clause to occursClause |
| VALUE THRU | NC201A, NC250A, NC252A | Add THROUGH in level-88 VALUE |
| STATUS reserved | NC174A, NC211A, NC254A | ON/OFF STATUS IS in SPECIAL-NAMES |
| PROGRAM reserved | NC215A, NC219A, NC114M, NC214M | Allow PROGRAM as paragraph name |
| INDEXED BY | NC133A, NC244A | Grammar fix for INDEXED BY parsing |
| Partial subscripts | NC138A, NC139A, NC245A | Allow fewer subscripts than OCCURS depth |
| Other grammar | 15 tests | Various parse issues |

No more tests can be fixed without grammar changes or new feature implementation.

---

