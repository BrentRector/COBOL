## Entry 341 — WS-SPEC scope clarification: M1 ('85) vs M2 (2002+); 10 spec-bugs fixed, the rest split

Stepping back after 10 backlog fixes (DEVLOG 333–340, the tractable '85 tier): the WS-SPEC agents audited against
the **ISO 2023** spec, so `docs/SPEC_FIX_BACKLOG.md` mixes '85 and post-'85 features. Per the owner's
**features-after-M1** decision, the 2002+ items are **deferred to M2**: `REPOSITORY … INTRINSIC`, HIGHEST/
LOWEST-ALGEBRAIC, multi-char `CURRENCY … WITH PICTURE SYMBOL`, `INITIALIZE WITH FILLER`/`ALL TO VALUE`, `REPLACE
ALSO`/`OFF LAST` (2014), `CALL RETURNING`/`BY VALUE`, the `(ALL)` intrinsic-arg subscript. The genuinely-'85
M1-remaining is small and deep/grammar: COMP-4 token, table-`(ALL)`-over-ODO (if '85), variable-length SORT/MERGE,
SORT Format-2 elementary self-key, cross-program GLOBAL FD I/O — plus **WS-SPEC-RW** (Report Writer SUM/control
breaks). Tagged in the backlog. **So the M1 spec axis is far closer to done than "20 fixes" implied**; the
deep/grammar '85 items + WS-SPEC-RW are best done with fresh context. (Note: two of the 10 done — CURRENCY
letter-symbol, CONCAT — are themselves 2002/2023 niceties, fixed opportunistically since they're harmless and
CLI-verified; they don't derail M1.)

