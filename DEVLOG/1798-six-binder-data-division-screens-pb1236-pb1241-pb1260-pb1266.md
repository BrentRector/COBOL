## Entry 1798 — 2026-09-29 20:21 PDT — Six binder / data-division screens: PB1236 + PB1241 + PB1260 + PB1266 + PB1264 + PB1145

A six-bug checkpoint (owner: 5 to 10 bugs per checkpoint, one gate). Each fix sits at the one place its rule is judged.

- **PB1236** — the SR2 uniqueness screen compared externalized names ordinally while the run-unit store keys case-insensitively, so `EXTERNAL` and `EXTERNAL AS "a"` aliased one cell in silence; it now asks the store's question (COBOLNET2159). And the GR6 VALUE identity upper-cased the literal's CONTENT, so `"ABCD"` and `"abcd"` compared identical: `ValueSpecificationKey` folds only keywords and hex digits.
- **PB1241** — a file-section VALUE set the initial length of a dynamic-length item; §13.4.4 GR1 says it takes no effect (initial length zero) except in INITIALIZE.
- **PB1260** (half) — an occurs-depending table under OCCURS DYNAMIC passed SR1 b) because the ancestor test was `Occurs is not null` (now `IsTable`); an eight-subscript table compiled (COBOLNET2646). The third leg, SR1 a) (OCCURS at level 01/77, a `NotAtLevel` placement row), was written, measured and BACKED OUT: 16 gate reds showed the corpus itself uses level-01 OCCURS in 13 goldens and dozens of inline tests, all non-conforming — re-deriving them is its own checkpoint, recorded on the note.
- **PB1266** — an index-name in ADD / CALL USING drew COBOLNET1639 "not defined": `ReportUnidentified` now names the category (COBOLNET1637); the KEY-after-INDEXED-BY hint no longer claims the clause is unsupported.
- **PB1264** — an OCCURS DYNAMIC FROM/TO above the implementor maximum compiled to capacity 0: COBOLNET2647, with the maximum written into DOC-A.1-60; the CAPACITY register also may not share a file-name.
- **PB1145** — the header formal / RETURNING screen was written twice with different subsets and neither tracked repeats; `ProcedureHeaderScreen` is now the one screen (COBOLNET2648) and its Roslyn-crash shapes are diagnostics.

Read and deliberately not taken: PB1143 (native MULTIPLY wraps once the scaled operands pass 38 digits — the scale is a compile-time constant, so an exact wide product needs the intermediate carrier redesigned, not a checked multiply), PB1175 (a shared-storage table SORT is an implementation, not a screen), PB1114 (a float formal BY CONTENT to a nested activation needs a new crossing class). Notes left HALF: PB1260 (SR1 a) and its corpus sweep), PB1266 (FMT-13.18.38.2 is co-owned with PB1265) and PB1264 (SR30's paragraph-name half; SR28 not re-measured). GAP 794 to 784.

