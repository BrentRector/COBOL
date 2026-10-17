*>--- conformance-case
*> id: pb358-alternate-key-source
*> rules: [SR-12.4.5.6.3-6, GR-12.4.5.6.4-2, SR-12.4.5.12.3-5,
*>    GR-12.4.5.12.4-2, SR-14.9.30.3-11, SR-14.9.30.3-12,
*>    SR-14.9.41.3-7, GR-14.9.41.4-15]
*> source-format: free
*> portable: unverified
*>---
*> THE OTHER ARM, and it exists because Annex A.3 item 40 names both clauses in ONE sentence: "The
*> capability of specifying the SOURCE phrase of the RECORD KEY clause and ALTERNATE RECORD KEY clause is
*> dependent on the capabilities of the processor." A fix reaching only the prime key would be this
*> repository's most reproducible defect shape - one dispatch, two arms, one of them fixed - on a clause
*> that literally IS the other arm. The original finding named only the prime key; kb/Work PB293 caught it.
*> 12.4.5.6.2's general format (RENDERED from printed page 350, folio 320) prints the SAME brace group as
*> 12.4.5.12.2's: ALTERNATE, RECORD and SOURCE carry underline rules; KEY and both occurrences of IS do
*> not. 12.4.5.6.4 GR2 gives the repetition its meaning - "Record-key-name-1 defines a record key
*> consisting of the concatenation of all occurrences of data-name-2 in the order specified."
*> The PRIME key here is the ordinary, supported data-name-1 form and is deliberately legal: the only thing
*> this entry can be refused for is the alternate clause's SOURCE phrase, so a decline that fired on the
*> supported form instead would show up as the wrong clause being named rather than as a silent pass.
*> Before 2026-09-09 this spelling drew TWO generic COBOL0312 errors - one at SOURCE and one at the WITH of
*> a DUPLICATES phrase the parser had already lost its place before reaching. kb/Work PB358.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB358AKSRC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IX ASSIGN TO "pb358aksrc.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS F-K1
        ALTERNATE RECORD KEY IS F-ALT SOURCE IS F-K2 F-D.
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
