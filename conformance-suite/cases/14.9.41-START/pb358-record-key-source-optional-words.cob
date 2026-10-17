*>--- conformance-case
*> id: pb358-record-key-source-optional-words
*> rules: [SR-14.9.41.3-7, FMT-12.4.5.12.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.12.2, THE CONFORMING MINIMUM SPELLING of the declined key form, and the reason it
*> gets its own fixture: on printed page 359 (folio 329, RENDERED - the OCR'd diagrams were systematically
*> lossy toward falsely-restrictive syntax) only RECORD and SOURCE carry underline rules. KEY and BOTH
*> occurrences of IS are un-underlined, and 8.3.2.4.3 makes such a word one that "may be specified at the
*> user's option with no effect on the semantics of the format" - so "RECORD F-COMPOSITE SOURCE F-K1" is
*> the same clause as the fully-spelled one in pb358-record-key-source, and has to draw the same decline.
*> A grammar that only recognized "RECORD KEY IS ... SOURCE IS ..." would send this spelling back to the
*> generic COBOL0312 parse error while the fully-spelled twin was named - a decline with a hole in it.
*> ONE data-name-2 is also deliberate: 12.4.5.12.2 puts the ellipsis on the inner { data-name-2 } brace
*> pair, so ONE occurrence is the minimum the format admits, and 12.4.5.12.4 GR2's "concatenation of all
*> occurrences" degenerates to a single window - the case a reader might assume is really the data-name-1
*> arm. It is not: record-key-name-1 is a different name class (8.3.2.2.24) and is declined either way.
*> Annex A.3 item 40; docs/CONFORMANCE.md section 2 row 40; 4.2.6 paragraph 3. kb/Work PB358.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB358RKSRCOW.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IX ASSIGN TO "pb358rksrcow.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD F-COMPOSITE SOURCE F-K1.
DATA DIVISION.
FILE SECTION.
FD F-IX.
01 F-REC.
   05 F-K1 PIC X(2).
   05 F-K2 PIC X(2).
   05 F-D  PIC X(6).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F-IX
    CLOSE F-IX
    STOP RUN.
