*>--- conformance-case
*> id: pb358-alternate-key-source-optional-words
*> rules: [SR-14.9.41.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.6.2, THE CONFORMING MINIMUM SPELLING of the alternate clause's declined key form -
*> the ALTERNATE twin of pb358-record-key-source-optional-words, and here for the same reason: on printed
*> page 350 (folio 320, RENDERED) ALTERNATE, RECORD and SOURCE carry underline rules while KEY and BOTH
*> occurrences of IS do not, and 8.3.2.4.3 makes an un-underlined uppercase word one that "may be specified
*> at the user's option with no effect on the semantics of the format".
*> So "ALTERNATE RECORD F-ALT SOURCE F-K2" is the same clause as the fully-spelled twin and draws the same
*> named decline. RECORD is written because it IS underlined - the grammar's ALTERNATE RECORD? is a
*> documented superset leniency, and a spec-derived fixture pins the standard's spelling, never a leniency.
*> Annex A.3 item 40; docs/CONFORMANCE.md section 2 row 40; 4.2.6 paragraph 3. kb/Work PB358.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB358AKSRCOW.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IX ASSIGN TO "pb358aksrcow.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS F-K1
        ALTERNATE RECORD F-ALT SOURCE F-K2.
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
