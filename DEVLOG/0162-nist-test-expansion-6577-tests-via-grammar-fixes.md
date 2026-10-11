## Entry 162 — 2026-03-28: NIST Test Expansion — 65→77 Tests via Grammar Fixes

**Result:** 77 NIST tests at 100% (up from 65). 12 new tests pass: NC109M, NC113M, NC135A,
NC138A, NC174A, NC204M, NC215A, NC217A, NC218A, NC223A, NC246A, NC247A.

**Grammar fixes (approved by user before implementation):**
1. **DISPLAY UPON/NO ADVANCING/END-DISPLAY** (§14.9.11) — added `displayUpon` and
   `displayNoAdvancing` sub-rules. Unblocked NC204M, NC220M, NC401M parse stage.
2. **SET TO ON/OFF** (§14.9.39 Format 3) — new `setSwitchStatement` rule with compound
   form `SET sw-1 TO ON sw-2 TO OFF`. Unblocked NC174A.
3. **WRITE ADVANCING optional** — NIST tests use `WRITE ... AFTER 1` without ADVANCING keyword.
   Made ADVANCING optional in `writeBeforeAfter`. Unblocked NC113M.
4. **STRING/UNSTRING WITH optional** — `WITH` before `POINTER` made optional. Unblocked NC217A.
5. **STRING/UNSTRING OVERFLOW** — `ON` made optional before OVERFLOW. Added standalone
   `NOT OVERFLOW` alternative (no preceding ON OVERFLOW required).
6. **UNSTRING TALLYING IN optional** — `IN` made optional before dataReference.
7. **UNSTRING DELIMITED OR** — restructured `unstringDelimiterPhrase` into
   `unstringDelimiterItem (OR unstringDelimiterItem)*` for multi-delimiter support. Unblocked NC218A.
8. **DELIMITED BY optional** — `BY` made optional in both STRING and UNSTRING.
9. **UNSTRING DELIMITER/COUNT IN optional** — `IN` made optional in `unstringIntoTarget`.
10. **INSPECT FOR multiple count phrases** — changed `inspectForClause` from
    `FOR inspectCountPhrase` to `FOR inspectCountPhrase+`. Unblocked NC216A (partially).
11. **IS before symbolic operators** — added `IS?` prefix to all symbolic comparison operators
    (`EQUALS`, `GTEQUAL`, `LT`, etc.) and their NOT variants. `IF X IS >= Y` now parses.
12. **ACCEPT FROM mnemonic-name** — added `dataReference` alternative to `acceptSource`.

**Visitor updates:**
- `BoundTreeBuilder.cs`: Updated UNSTRING delimiter binding to iterate `unstringDelimiterItem[]`
  instead of accessing removed direct fields. Updated INSPECT tallying to iterate
  `inspectCountPhrase[]` array (was single context). Full OR-delimiter list bound but only first
  used at runtime (OR support deferred to runtime layer).

**Still failing (18 NC tests):**
- **Parse:** NC125A (PIC period), NC205A (continuation), NC216A (INSPECT replacing pattern),
  NC250A (ZERO arithmetic), NC302M (AUTHOR paragraph)
- **Semantic:** NC108M (VALUE incompatible), NC201A (PERFORM index subscripted), NC208A
  (qualified paragraph name), NC209A (REDEFINES level), NC225A (comparison normalization),
  NC401M (ArithExpr vs ArithExpr comparison)
- **Codegen:** NC252A (IL error)
- **Runtime:** NC220M, NC237A (known hangs)
- **No output:** NC110M, NC214M, NC219A, NC303M

---

