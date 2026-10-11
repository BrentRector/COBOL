## Entry 1831 — 2026-09-30 22:11 PDT — The known limits of train w0930 adjudicated against the ISO text: four licensed, one divergence (PB1890), one precedence defect (PB1900)

The owner asked for the limits named in the closing summary to be validated per the ISO spec. A read-only adjudicator (every citation run through `cite.py --check`; three first-try clause numbers were wrong and corrected: 12.3.7 to 12.3.7.4, 13.16 to 13.4.5.3) ruled on each, reading GnuCOBOL 3.2 from its source tarball and running nothing.

| item | verdict |
|---|---|
| dynamic members stored at maximum width, a trailing space reads back as padding (D-FRA (vi)) | IMPLEMENTOR-DEFINED and documented: the standard leaves a dynamic item's structure and location to the implementor (8.5.1.10.2, 13.18.19.3 SR3, A.1 item 63, 8.5.1.10.3) and no READ or WRITE rule says how a READ sets its length; GnuCOBOL 3.2 has no DYNAMIC LENGTH clause |
| LINE SEQUENTIAL with two dynamic members mis-splits (D-FRA (ii)/(v)) | IMPLEMENTOR-DEFINED and documented: no RECORD CONTAINS on a line sequential file (13.4.5.3 SR4), a line has no frame (9.1.7.2), no rule decomposes a plain line |
| nested product into a wide receiver keeps 34 digits (DOC-A.1-123) | licensed by ISO (8.8.1.3, 14.7.7 4) a), A.1 item 123) but it DEPARTS from the rule-1 precedence (GnuCOBOL multiplies exactly) and gives a wrong answer in plain ISO code (`A*B - C*D`, PIC 9(21): 10000000 for 1), so it is filed as **PB1900**, MAJOR |
| divide with a divisor scaled past the carrier | licensed and documented (DOC-A.1-123); GnuCOBOL's divide is inexact too |
| PB1890, subscript / ref-mod bound stored in the 9-fraction truncating temporary | native arm licensed (5.5 3) a), A.1 item 124); the STANDARD-DECIMAL / STANDARD-BINARY arm **DIVERGES** (5.5 3) b), 8.4.2.3.4 1) b), 8.4.3.3.4 5)): `T(IX + 0.0000000001)` must raise EC-BOUND-SUBSCRIPT. PB1890 now carries the adjudication and a fix design; deferring it to the Phase 15 carrier migration is not allowed (rule 3) |
| PB1880, five test gaps | ODO READ (three sub-cases) and WRITE WITH LOCK under a permanent error are conformance witnesses; the PB1562 relative/indexed round trip tests our documented choice; the national pad is spec-required only for LINE SEQUENTIAL (14.9.30.4 GR15) |

Two derivations (PB1890's standard-mode behavior and PB1900's cancellation case) come from reading the code, not from running it; each note says to probe first. No code changed. PB1890 and PB1900 are for a later wave; the owner stopped work for the night.

