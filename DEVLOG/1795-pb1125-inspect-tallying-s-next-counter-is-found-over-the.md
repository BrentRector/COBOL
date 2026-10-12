## Entry 1795 — 2026-09-29 19:18 PDT — PB1125: INSPECT TALLYING's next counter is found over the whole reference

`IsBareInspectOperand()` was `LA(2) != FOR`, so a SUBSCRIPTED, QUALIFIED or reference-modified next counter (`INSPECT S TALLYING N FOR ALL "A" C(1) FOR ALL "B"`) had `(` or `OF` at LA(2), was swallowed as one more operand of the previous counter, and its FOR clause re-attached to THAT counter — a silent wrong count at every edition (N=3, C(1) untouched where the standard gives N=2, C(1)=1). The predicate now scans the reference's own extent and asks whether FOR follows. Closes GR-14.9.22.4-10; GAP 797 to 796. FMT-14.9.22.2 (the printed formats' over-generation) needs the §14.9.22.2 diagram rendered first and stays with the note (HALF).

