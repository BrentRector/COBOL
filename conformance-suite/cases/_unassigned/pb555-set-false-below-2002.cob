*>--- conformance-case
*> id: pb555-set-false-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB555 — arm: the EDITION EDGE below the introducing edition. The VALUE clause's
*> `WHEN SET TO FALSE` phrase (ISO 13.18.63.2 format 3 / 13.18.63.4 GR20) and the
*> `SET condition-name TO FALSE` arm it feeds (ISO 14.9.39.2 Format 4 / 14.9.39.4 GR7) are a COBOL-2002
*> introduction; COBOL-85's Format 4 carries the TRUE arm only.
*> ⛔ THE EDGE IS DERIVED, NOT QUOTED, and the derivation is one line so it can be overturned in one:
*> the repo holds no 2002 or 2014 text, the 2023 Annex E carries no 85->2002 VALUE or SET row (the A.1
*> authority gap, D18), and the reserved-word evidence cannot date it because FALSE is already reserved
*> at COBOL-85 for the EVALUATE statement.
*> This program is CONFORMING at 2002 and above — the same shape the 2002 corpus runs — so what it pins
*> is the gate alone. Rows value-false-phrase-2002 / set-condition-false-2002 carry the matrix cells;
*> this case pins that the gate fires from SOURCE RECOGNITION and is not lost when the binder proceeds.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB555D.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CV PIC 9 VALUE 1.
   88 CN VALUE 1 WHEN SET TO FALSE IS 7.
PROCEDURE DIVISION.
    SET CN TO FALSE.
    DISPLAY CV.
    STOP RUN.
