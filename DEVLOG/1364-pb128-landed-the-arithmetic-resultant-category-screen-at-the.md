## Entry 1364 — 2026-08-29 02:53 PDT — PB128 LANDED: the arithmetic RESULTANT category screen at the ONE receiving chokepoint, and the four bypasses rerouted through it

Batch 8's widest single find. No arithmetic statement screened its resultants' categories — `COMPUTE X-item
= 1`, `ADD 1 TO ZZZ9`, `DIVIDE 2 INTO X-item` all compiled and died in StoreArith's run-time loud, where
§4.2.2 requires a compile-time mechanism and the SENDING side has had DA6's screen for a month. The screen
(COBOLNET1675) lives in ScreenResultant beside ResolveReceiving, threaded through the three Receivers
overloads with the RULE STATED AT EVERY CALL SITE: in-place receivers (ADD TO §14.9.2.3 SR2, SUBTRACT FROM
§14.9.44.3 SR2, MULTIPLY BY §14.9.26.3 SR1, DIVIDE INTO §14.9.12.3 SR1) are elementary-numeric-only; the
GIVING resultants, DIVIDE's REMAINDER (SR2 — numeric-edited IS admitted there, pinned from the legal side in
the golden) and COMPUTE's identifier-1 (§14.9.8.3 SR1) admit numeric-edited. An index DATA item
(§13.18.60.3 SR10's closed list) and a ref-mod slice (§8.4.3.3.4 GR6c — category alphanumeric) reject in
the same arm.

The four chokepoint BYPASSES rerouted: DIVIDE's REMAINDER identifier-4, CALL's BY REFERENCE and bare
arguments and its RETURNING identifier-3, and COMPUTE Format 2's boolean receivers (the arithmetic screen
deliberately not applied there — §14.9.8.3 SR2 is PB157's) — so the CONSTANT RECORD (§13.18.15.3 SR2),
CAPACITY-register (§13.18.38 SR30–32), constant-name and LINE-COUNTER screens now reach them; a CONSTANT
RECORD item as a CALL BY REFERENCE argument was silently overwritable, and a CAPACITY register there was an
UNHANDLED COMPILER EXCEPTION (PlaceRenderer.Write's internal throw). SR-14.9.8.3-1, SR-14.9.2.3-2,
SR-14.9.12.3-2 → CONFORMS; SR-14.9.4.3-5 → PARTIAL (the object-data and once-only halves stay with
PB132/PB133).

