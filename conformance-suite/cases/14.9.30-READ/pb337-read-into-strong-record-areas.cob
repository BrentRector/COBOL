*>--- conformance-case
*> id: pb337-read-into-strong-record-areas
*> rules: [SR-14.9.30.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.30.3 syntax rule 2: "If identifier-1 is a strongly-typed group item, there
*> shall be at most one record area subordinate to the FD for file-name-1.  This record area, if
*> specified, shall be a strongly-typed group item of the same type as identifier-1."
*> This FD has TWO record areas, so the rule's COUNT obligation is broken outright -- COBOLNET1995.
*> THE COUNT IS A SEPARATE OBLIGATION FROM THE SAME-TYPE ONE, and only the count needs a screen
*> here.  The rule's second sentence is the same predicate over the same pair that 14.9.25.3 SR2
*> applies to this phrase's implicit-move SENDER -- which IS the record area -- and is reported
*> there, COBOLNET1533 (see pb348-read-into-strong-group).  But that screen inspects only ONE
*> record, FileModel.AreaRecord, so a file whose LARGEST record happened to be a strongly-typed
*> group of the right type passed everything: no diagnostic could see the second area at all.
*> This fixture is the one the same-type screen cannot catch, kept deliberately DISTINCT from its
*> PB348 sibling so that removing either check fails a test.
*> 85 is not listed: TYPEDEF and the STRONG phrase are 2002 introductions, so the 1985 leg is
*> refused by the edition gate (COBOLNET0900) rather than by syntax rule 2.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB337N3.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb337n3.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 F-REC-1 PIC X(4).
01 F-REC-2 PIC X(4).
WORKING-STORAGE SECTION.
01 T1 IS TYPEDEF STRONG.
   05 T1-A PIC X(4).
01 WS-STRONG TYPE T1.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    READ SQF INTO WS-STRONG
        AT END CONTINUE
    END-READ
    CLOSE SQF
    STOP RUN.
