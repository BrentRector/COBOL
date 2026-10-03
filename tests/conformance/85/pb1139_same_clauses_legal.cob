      *> kb/Work PB1139 - THE LEGAL SPELLINGS NEXT TO THE SAME-CLAUSE RULES COMPILE (compile-only; the refusals are the
      *> negatives pb1139-sort-same-sort-area, -sort-giving-same-area, -merge-same-area, -merge-same-record-area).
      *>   cite.py --check 14.9.24.3 "The only file-names in a MERGE statement that may be specified in the same SAME
      *>     RECORD AREA clause are those associated with the GIVING phrase." -> OK §14.9.24.3 11)
      *>   cite.py --check 14.9.40.3 "No pair of file-names in the same SORT statement may be specified in the same
      *>     SAME SORT AREA or SAME SORT-MERGE AREA clause." -> OK §14.9.40.3 10)
      *> LEGAL: (1) a MERGE whose two GIVING files share a record area; (2) a SORT whose USING and GIVING files share
      *> a record area (SR10 bars SAME SORT AREA / SAME SORT-MERGE AREA pairs and SAME AREA among GIVING files, not
      *> SAME RECORD AREA); (3) a SAME SORT AREA clause naming only ONE of the SORT's file-names (F5 is not in either
      *> statement) - the rule is about a PAIR OF THE STATEMENT'S file-names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb1139ok.tmp".
           SELECT F1 ASSIGN TO "pb1139ok1.dat".
           SELECT F2 ASSIGN TO "pb1139ok2.dat".
           SELECT F3 ASSIGN TO "pb1139ok3.dat".
           SELECT F4 ASSIGN TO "pb1139ok4.dat".
           SELECT F5 ASSIGN TO "pb1139ok5.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F2 F3.
           SAME SORT AREA FOR SW F5.
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC X(4).
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       FD F3.
       01 R3 PIC X(4).
       FD F4.
       01 R4 PIC X(4).
       FD F5.
       01 R5 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY SK USING F1 F4 GIVING F2 F3
           SORT SW ASCENDING KEY SK USING F2 GIVING F3
           STOP RUN.
