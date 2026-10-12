## Entry 656 — 2026-07-07 02:54 PDT — Phase 6: OCCURS DYNAMIC increment 5 — the staged-loud guards (1522/1525/1527/1528); ⛔🎉 OCCURS DYNAMIC COMPLETE

The final increment of **OCCURS DYNAMIC** (data-model **D9**): the construction/placement rules whose violation must
be a LOUD rejection rather than a silent mis-compile. Greenfield-only. **Each guard was TESTED against the actual
current behavior first** (the verify-by-running discipline) — three of the four candidate cases were found to compile
SILENTLY (a wrong-answer bug), one was found already-correct:

- **1522** (`DynamicResolve`) — §13.18.38 SR28 (spec line 19987): `FROM 8 TO 3` compiled silently (a table opening
  above its own expected capacity); now rejected — TO shall be greater than FROM.
- **1525** (`ClassifyRedefinesClasses`, via a new `ContainsDynamicTable` subtree walk) — §13.18.44 SR5 (:21497):
  `05 WS-R REDEFINES WS-E` over a dynamic table compiled silently AND `WS-R` got its OWN storage (the REDEFINES did
  NOT share `WS-E`'s out-of-line `CobolDynTable` — semantically broken); now rejected + the class forced `Rejected`.
- **1528** (`DynamicResolve`) — §13.18.38 GR16 / §13.18.63 GR6 (:24102): a VALUE on an ELEMENTARY dynamic entry
  (`05 WS-E … OCCURS DYNAMIC FROM 3 VALUE 7`) DERIVES the initial capacity per the exception rule ("except where the
  table is defined by an elementary entry with a VALUE clause") — the greenfield instead silently seeded the element to
  7 and took capacity from FROM; now staged loud. **Precisely scoped:** a VALUE on the SUBORDINATE of a GROUP dynamic
  table (`10 WS-NAME VALUE "----"`) is the element's per-occurrence seed (capacity = FROM), which IS supported —
  the guard gates on `IsElementary && RawValue != null`, so the `dyn_initialize`/`dyn_initialized` group goldens do NOT
  false-trip.
- **1527** — the containing-group INITIALIZE loud message now carries the code; the whole-dynamic-table value op stays
  the inc-2 runtime `NotImplemented` loud (the §14.6.9 variable-length-group family).

**1526 was found UNNECESSARY and SKIPPED (design refinement, recorded in D9):** reference modification of a dynamic-
table element empirically WORKS (`WS-E(i)(1:2)` → the right substring, a `RefModPlace` over the inc-3 `DynTablePlace`),
and the cited "§13.7.1 SR6" is actually the §8.4.3.11.4 ADDRESS-OF/bit-item SR6 — not a general ref-mod prohibition —
so a 1526 guard would have over-restricted valid code. This is exactly the kind of over-restriction the "test the
actual behavior first" discipline catches.

Negative tests: `OccursDynamicGuardTests` (1522/1525/1528 rejections + the two positive companions — a well-formed
FROM/TO and the group-subordinate VALUE — compile clean). Two follow-ons remain FLAGGED (both LOUD today, never
silently wrong): EC-BOUND-OVERFLOW (nonfatal, checking-gated) and the full variable-length-group MOVE/COMPARE +
FUNCTION LENGTH (a `DynWholeTablePlace` giving `Capacity × elemWidth`).

**⛔🎉 OCCURS DYNAMIC (D9) is now COMPLETE** across all 5 increments: declaration + the growable `CobolDynTable<T>`
substrate (652) · the CAPACITY register + SET Format 14 (653) · subscripted element access + implicit growth (654) ·
SEARCH over current capacity + INITIALIZE (655) · the staged-loud guards (656). Greenfield battery green throughout;
the diagnostic band is 15xx through 1528. NEXT (D9 §0.5 / roadmap): the next Phase-6 track — TYPEDEF, or the M2-OO-1i
method-own-FILE-SECTION residue, or Phase 7 (2023 finalization).

