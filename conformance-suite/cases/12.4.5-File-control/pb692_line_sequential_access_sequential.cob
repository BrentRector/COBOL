      *>--- conformance-case
      *> id: pb692_line_sequential_access_sequential
      *> rules: [SR-12.4.5.5.2-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 12.4.5.5.2 SR2 bans DYNAMIC and RANDOM for a sequential
      *> file -- and NOTHING ELSE. This is the COMPLEMENT of that
      *> screen: the one legal (organization x access mode) cell the
      *> corpus did not already contain. A static scan of all 1483
      *> SELECT entries in tests/ found LINE SEQUENTIAL paired with an
      *> EXPLICIT "ACCESS MODE IS SEQUENTIAL" exactly ZERO times, so
      *> the LineSequential arm of the screen's organization predicate
      *> had no positive witness at all; a screen is evidence about
      *> what it rejected, never about what it let through.
      *> Rules and derivation of every expected line:
      *>  - 12.4.5.10.3 GR2 puts LINE SEQUENTIAL in the ORGANIZATION
      *>    clause and 12.4.5.2 SR11 makes the entry carrying it one
      *>    "for a sequential file"; 12.4.5.5.2 SR2 bans only DYNAMIC
      *>    and RANDOM there, so ACCESS MODE IS SEQUENTIAL is legal
      *>    and the program COMPILES.
      *>  - 12.4.5.5.3 GR2 a): under sequential access the records of
      *>    a sequential file come back in the order the WRITEs
      *>    established. Two records are written, so they read back
      *>    ALPHA then BRAVO.
      *>  - 9.1.13.2 item 1: an input-output statement "successfully
      *>    executed and no further information is available" is I-O
      *>    status '00' -- OUT/CLS/IN/CLS2 are all 00.
      *>  - 14.9.30.4 GR21: with no next logical record the at end
      *>    condition exists, and 9.1.13.4 item 1 gives that '10', so
      *>    the third READ takes its AT END branch and prints EOF=10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB692P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FL ASSIGN TO "pb692p1.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-L.
       DATA DIVISION.
       FILE SECTION.
       FD FL.
       01 L-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST-L PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FL
           DISPLAY "OUT=" ST-L
           MOVE "ALPHA" TO L-REC
           WRITE L-REC
           MOVE "BRAVO" TO L-REC
           WRITE L-REC
           CLOSE FL
           DISPLAY "CLS=" ST-L
           OPEN INPUT FL
           DISPLAY "IN=" ST-L
           READ FL
               AT END DISPLAY "EOF=" ST-L
           END-READ
           DISPLAY "R1=" L-REC
           READ FL
               AT END DISPLAY "EOF=" ST-L
           END-READ
           DISPLAY "R2=" L-REC
           READ FL
               AT END DISPLAY "EOF=" ST-L
           END-READ
           CLOSE FL
           DISPLAY "CLS2=" ST-L
           STOP RUN.
