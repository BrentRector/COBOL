*>--- conformance-case
*> id: pb337-read-into-not-admissible
*> rules: [SR-14.9.30.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.30.3 syntax rule 1: "The INTO phrase may be specified in a READ statement:
*> a) If no record description entry or only one record description is subordinate to the file
*> description entry, or b) If the data item referenced by identifier-1 and all record-names
*> associated with file-name-1 describe an alphanumeric group item or an elementary item of category
*> alphanumeric or category national."
*> NEITHER arm holds here: the FD carries TWO record descriptions, and identifier-1 (WS-NUM) is an
*> elementary item of category NUMERIC.  The rule exists because the two record descriptions share
*> ONE record area (13.4.2) and the phrase moves that area (14.9.30.4 GR4 b), so which description
*> the area holds is a run-time fact -- the only way the move's category can be known at compile
*> time is for every description, and the receiver, to be a shape a group move copies without
*> conversion.  COBOLNET1994.
*> Before kb/Work PB337 this program compiled and printed GOT=0000: the INTO operand was resolved
*> and never inspected, by either READ binder or by RETURN's.
*> All four editions are listed: the admissibility rule is an all-editions rule (it is the 1985
*> standard's rule too, worded "a group item or an elementary alphanumeric item"), and the two
*> shapes that make the 2023 wording narrower -- an ALPHANUMERIC group item rather than any group,
*> and category national -- cannot be declared below 2002 anyway.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB337N1.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN TO "pb337n1.dat"
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 REC-A.
   05 A-1 PIC X(4).
01 REC-B.
   05 B-1 PIC 9(4) COMP.
WORKING-STORAGE SECTION.
01 WS-NUM PIC 9(4) COMP.
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT SQF
    READ SQF INTO WS-NUM
        AT END CONTINUE
    END-READ
    CLOSE SQF
    STOP RUN.
