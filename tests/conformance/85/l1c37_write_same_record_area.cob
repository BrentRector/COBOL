      *> ISO §14.9.51.4 GR4 + GR9 — after WRITE the record stays
      *>   available through a SAME RECORD AREA peer, and the FROM
      *>   operand is untouched
      *> GR4: "The logical record released by the successful execution
      *>   of the WRITE statement is no longer available in the record
      *>   area unless the file-name associated with record-name-1 is
      *>   specified in a SAME RECORD AREA clause. The logical record is
      *>   also available as a record of other files referenced in the
      *>   same SAME RECORD AREA clause as the associated output file,
      *>   as well as the file associated with record-name-1."
      *>   cite.py --check 14.9.51.4 "The logical record is also
      *>   available as a record of other files referenced in the same
      *>   SAME RECORD AREA clause as the associated output file, as
      *>   well as the file associated with record-name-1"
      *>   -> OK  §14.9.51.4 4)  (General rules)
      *> GR9: "After the successful execution of a WRITE statement, the
      *>   information in the area referenced by identifier-1 is
      *>   available, provided that identifier-1 is not one or part of
      *>   one of the record descriptions subordinate to the
      *>   file-description ..."
      *>   cite.py --check 14.9.51.4 "the information in the area
      *>   referenced by identifier-1 is available, provided that
      *>   identifier-1 is not one or part of one of the record
      *>   descriptions subordinate to the file-description"
      *>   -> OK  §14.9.51.4 9)  (General rules)
      *> DERIVATION. F1 and F2 share one record area (SAME RECORD AREA
      *> FOR F1 F2), both open OUTPUT.
      *>   WRITE R1 FROM WS-SRC ("ABCDEFGH"): GR5 a) moves it to R1,
      *>   the record is released; WS-SRC is a working-storage item, so
      *>   GR9 keeps it available              -> "SRC ABCDEFGH"
      *>   GR4: the released record remains available as R1 (the file
      *>   of record-name-1)                    -> "R1 ABCDEFGH"
      *>   and as F2's record R2, split 4|4     -> "R2 ABCD|EFGH"
      *>   A second WRITE R1 FROM WS-XY ("XY"): GR5 a) pads to
      *>   "XY      "; the peer shows it too     -> "R2 XY  |    "
      *>   (an identifier: FROM literal-1 is a COBOL-2002 addition,
      *>   refused at 85 - kb/Work PB2650)
      *> The "no longer available" half of GR4 (a file WITHOUT the
      *> clause) leaves the area's content undefined and is not pinned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C37F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C37F1.DAT".
           SELECT F2 ASSIGN TO "L1C37F2.DAT".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1.
           05 R1-X PIC X(8).
       FD F2.
       01 R2.
           05 R2-A PIC X(4).
           05 R2-B PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-SRC PIC X(8) VALUE "ABCDEFGH".
       01 WS-XY  PIC X(2) VALUE "XY".
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT F1 F2
           WRITE R1 FROM WS-SRC
           DISPLAY "SRC " WS-SRC
           DISPLAY "R1 " R1-X
           DISPLAY "R2 " R2-A "|" R2-B
           WRITE R1 FROM WS-XY
           DISPLAY "R2 " R2-A "|" R2-B
           CLOSE F1 F2
           STOP RUN.
