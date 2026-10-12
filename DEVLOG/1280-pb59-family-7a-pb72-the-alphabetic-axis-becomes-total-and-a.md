## Entry 1280 — 2026-08-09 17:48 PDT — PB59 family 7a + PB72: the alphabetic axis becomes total, and a rider carries what a category cannot

The scan-all-similar sweep that opened family 7 grew it, exactly the way rule 5 says an estimate grows:
reading Table 16 AS PRINTED against eight CLI probes found the alphabetic/edited/noninteger axes
PARTIAL — `MOVE 5 TO PIC A` accepted (storing "5   " into a letters-only item), `MOVE PIC-A TO PIC 9`
accepted (storing zeros), alphanumeric-edited→numeric accepted, `MOVE 5.5 TO PIC X` accepted — while
the ref-mod ERASURE ran the other way: §8.4.3.3.4 GR2/GR6 (with GR1+SR5's closed class lists) make
every usage-DISPLAY view plain class-and-category alphanumeric, so `MOVE A-ITEM(1:2) TO a-boolean` and
the edited twin — Table 16's plain alphanumeric→boolean "Yes" — were being REFUSED off the inner
item's flags. Registered as PB72 before a line of code moved (rule 8), then landed in one change set
with family 7's rider, because they are the same axis at the same edit sites.

The landed shape: four new arms complete `MoveTable16.Refusal` over the printed table's modeled
categories; `Table16Operand.Of(Place)` is the ONE position builder and erases the finer flags for a
view (category through the ONE GR6 reader); both MoveBinder positions and OoBinder's BY CONTENT
crossing build through it (the Place now travels — `.Item` had discarded the view). CONCAT's §15.18.4
r3 ALPHABETIC result rides `BoundIntrinsicCall.ResultIsAlphabetic` — a rider like Collate, never a
`PicCategory.Alphabetic` (the PIC A fold's written-down decisions hold; the rider is per-call derived
state no §15.3 rule can name) — set at the one generic construction site (every argument a non-ref-mod
PIC A field or a rider-carrying nested CONCAT), consumed at Table 16's sender position:
`MOVE FUNCTION CONCAT(picA picA) TO boolean` now rejects while plain/mixed/ref-mod forms stay
admitted. Place.cs's NOTE claiming ref-mod "preserves both" is corrected with the derivation.

The gate earned its keep twice. The corpus caught an UNGUARDED IsEdited arm refusing the de-editing
numeric-edited→numeric move (`move_numeric_edited_source` went red at wave-local — IsEdited is set
from the ONE EditMask field, so the alphanumeric-edited row needs its category guard); and the manifest
hook caught every unregistered golden at write time. Evidence: eight probes before and after; corpus
golden `pb72_table16_admitted_cells` (RA/RB/XB/MX), five negative fixtures; wave-local Conformance
1018/1018 (the five negatives and the golden in the population), Unit 87/87, SpecTraceability 10/10.
Verdicts: RV-15.18.4-3 → CONFORMS (PB59 at 27 of 28), SR-14.9.25.3-10 blank → CONFORMS. GAP 4159 →
4157. The two open DERIVATIONS (the §15.4 noninteger-function-sender question, which touches item 92;
the GR2 display-boolean view question, where GR1's explicit boolean positions are a real asymmetry)
are split to PB73 as adjudications, not silently absorbed. A comprehensive battery is owed NOW — the
MOVE seam is shared, and the differential may legitimately flip GnuCOBOL-corpus cases to
WE_REJECT_THEY_ACCEPT, each needing attribution and a baseline regen.

