*>--- conformance-case
*> id: pb743-alternate-key-numeric
*> rules: [SR-12.4.5.6.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.6.3 SR2, the CATEGORY arm - "Data-name-1 and data-name-2 shall be defined as
*> a data item of category alphanumeric or national within a record description entry associated with
*> the file-name to which the ALTERNATE RECORD KEY clause is subordinate."
*> The ALTERNATE twin, and it must ship WITH the prime one: the compiler already wrote a category
*> screen for the RELATIVE key (12.4.5.13.3 SR2, an unsigned integer) and for neither record key -
*> one member of a rule set written down is exactly where the missing members hide (kb/Work PB743).
*> The prime key here is legal, so the only rejection is the alternate clause's.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB743AKNUM.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT IXF ASSIGN TO "pb743aknum.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS RANDOM
        RECORD KEY IS IX-KEY
        ALTERNATE RECORD KEY IS IX-ALT.
DATA DIVISION.
FILE SECTION.
FD IXF.
01 IX-REC.
   05 IX-KEY PIC X(5).
   05 IX-ALT PIC 9(3).
   05 IX-DATA PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT IXF
    CLOSE IXF
    STOP RUN.
