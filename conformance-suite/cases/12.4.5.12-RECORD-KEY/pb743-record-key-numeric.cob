*>--- conformance-case
*> id: pb743-record-key-numeric
*> rules: [SR-12.4.5.12.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.12.3 SR2, the CATEGORY arm - "Data-name-1 and data-name-2 shall reference a
*> data item of category alphanumeric or category national within a record description entry
*> associated with the file-name specified in this file control entry."
*> The sentence joins TWO obligations with the word "within": a CATEGORY and a LOCATION. Only the
*> location half was screened, so a PIC 9(5) prime key compiled clean, ran, and built an index on an
*> operand the standard forbids as a key (kb/Work PB743, measured: OPEN=00 / WRITE=00, zero
*> diagnostics). IX-KEY here satisfies the LOCATION half exactly - it is inside this file's record
*> description - which is what makes the case a discriminator for the category arm alone.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB743RKNUM.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb743rknum.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS RANDOM
        RECORD KEY IS IX-KEY.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC 9(5).
   05 IX-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
