## Entry 549 — 2026-06-10 15:00 PDT — Benign out-of-range chains through null tables → NC401M byte-matches (86/95)

NC401M (the obsolete-elements program — ALTER, CORR, ACCEPT DAY-OF-WEEK, debug lines: everything else already
worked after the verb wave) NRE'd in `CobolTable.At`: its 5-deep FAIL-path read chains subscripts through a
zeroed scratch struct after an out-of-range OUTER subscript, and the scratch's nested OCCURS arrays are null —
`At(null, …)` then dereferenced `table.Length`. Per the §8.4.2.3.4 GR2 benign-OOR rule the whole CHAIN must
resolve benignly: `At` now treats a null table as out-of-range (the documented zeroed-scratch caveat in its
doc-comment is hereby half-discharged — group-level scratch use still yields nulls for STRING members; the next
corpus case decides whether per-type construction is worth it). 86/95 NC GREEN; 567 conformance + 15 unit.

