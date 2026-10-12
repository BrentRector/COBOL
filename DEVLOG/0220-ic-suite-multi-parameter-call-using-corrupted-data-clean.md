## Entry 220 — 2026-05-29: IC suite — multi-parameter CALL USING corrupted data (CLEAN 10→16)

`CALL "IC104A" USING GROUP-01 ELEM-77 GROUP-02` returned garbage — IC103A read `2EAB` where the
subprogram had written `IC104`. Root cause: StorageLayoutComputer laid out EVERY LINKAGE 01/77
item starting at offset 0, so all USING parameters occupied overlapping offset ranges
([0,len1), [0,len2), …). `FindLinkageField`, which identifies a parameter by the offset range
containing a reference, therefore resolved every 2nd/3rd-parameter reference to the FIRST
parameter — so the subprogram's writes to ELEM-01 and GRP-02 clobbered GRP-01. (Single-parameter
CALLs had no ambiguity, which masked the bug.)

Fix: lay out LINKAGE 01/77 items CONTIGUOUSLY (each after the previous) so every item has a unique
offset and `FindLinkageField` resolves the correct parameter. The per-parameter displacement is
then recovered at emit time as `item offset − parameter base offset` in `EmitLinkageBufferAndOffset`
(the CobolDataPointer for each parameter still addresses that argument's caller storage). The two
changes are interdependent: unique offsets disambiguate the parameter, and the base-offset
subtraction converts the now-cumulative offset back to a within-parameter displacement.

IC CLEAN 10→16, FAIL* 10→5. Guard ALL GREEN (1000 / 336 / 149) — single-parameter CALLs and all
existing LINKAGE access are unchanged (first parameter's base offset is 0).

