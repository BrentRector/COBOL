## Entry 1404 — 2026-08-30 17:19 PDT — PB180 + PB181: the two live Tier-B lane defects — one decode-reencode shape, measured before believed

Step D's second step, and the two defects turned out to be the same defect wearing two verbs. The
Tier-B REDEFINES window has held TRUE BYTES since V59, but two boundary stores still believed the
pre-V59 world where the window held display characters: ACCEPT spliced DisplayTextWidth device
characters into a StorageWidth window (PB180 — "the characters ARE the storage", a comment that
outlived its truth by two phases), and the CALL boundary's write-back spliced the returned display
image raw (PB181). Both fixes are the one store shape the MOVE path already uses: decode the display
image per the receiver's profile, narrow, re-encode through FormatImage.

PB181 earned a methodological note: the design scout claimed the READ half corrupted; the PB176
skeptic's report implied it was fine. Rather than adjudicate testimony, the probe ran — 1234 crossed,
the callee added one, and 2594 came home with ORD 50/51 showing the display characters sitting in the
byte window. The read half WAS correct (the boundary's display-image convention is how native↔native
works); the write-back was the liar. Measured, fixed, re-measured: 1235, bytes 04 D3, the sibling
untouched.

Two discriminating pins: the ACCEPT fact feeds stdin and reads the window's bytes back through ORD on
the redefined view; the CALL golden does the full round trip. Wave gate GREEN (Conformance 1343 — the golden and the ACCEPT fact by name · Unit 5032 · characterization 33). Step D's remaining step: the
arm-1 dissolution with its lanes. Battery #37 accrues.

