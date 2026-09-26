       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB690LSQ.
      *> ISO/IEC 1989:2023 §14.9.51.4 GR23 / §14.9.35.4 GR17 d) /
      *> §9.1.13.10 item 1 — the line sequential character set (Annex
      *> A.1 item 115) meets the file coded character set (Annex A.1
      *> item 31; owner decision kb/Work R47; kb/Work PB690).
      *>
      *> GR23: "For a line sequential file, if the record area contains
      *> one or more characters that are not in the implementor-defined
      *> character set defined for a line sequential file, the
      *> execution of the WRITE statement is unsuccessful and the I-O
      *> status in the write file connector is set to '71'."
      *> §9.1.13.10 1): "... The write or rewrite operation was
      *> unsuccessful and the record area remains unchanged."
      *>
      *> The determination (docs/CONFORMANCE.md DOC-A.1-115): in an
      *> ALPHANUMERIC record area the set is U+0020 through U+00FF — a
      *> character above U+00FF has no byte image in the file's coded
      *> character set, so it is outside the set and the WRITE/REWRITE
      *> is the standard's '71' (every other organization answers the
      *> implementor's '91'); a NATIONAL record area is written as
      *> UTF-16BE and keeps no ceiling. ORGANIZATION LINE SEQUENTIAL is
      *> a COBOL-2023 introduction, so this golden exists at 2023 only.
      *>
      *> Why each leg can fail:
      *>  LS-EURO - '71' (the defect wrote "A?BC" with '00').
      *>  LS-E    - U+00E9 is in the set: '00'.
      *>  LS-READ - the file holds exactly the one line, byte 2 = 0xE9
      *>            (ORD 234), then at end.
      *>  LS-REWR - REWRITE of a record area holding U+20AC is '71' and
      *>            the line is unchanged (LS-AGAIN).
      *>  NL-*    - a national record area holding U+20AC is written
      *>            and read back whole: '00' and the character itself.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LS ASSIGN TO "pb690lsq-a.txt"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS FL.
           SELECT NL ASSIGN TO "pb690lsq-n.txt"
               ORGANIZATION LINE SEQUENTIAL FILE STATUS FN.
       DATA DIVISION.
       FILE SECTION.
       FD LS.
       01 LR PIC X(4).
       FD NL.
       01 NR PIC N(2).
       WORKING-STORAGE SECTION.
       01 FL PIC XX.
       01 FN PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT LS.
           MOVE "A€BC" TO LR.
           WRITE LR.
           DISPLAY "LS-EURO=" FL.
           MOVE "AéBC" TO LR.
           WRITE LR.
           DISPLAY "LS-E=" FL.
           CLOSE LS.
           OPEN INPUT LS.
           READ LS.
           DISPLAY "LS-READ=" FL " " FUNCTION ORD(LR(2:1)).
           READ LS AT END DISPLAY "LS-READ2 AT END".
           CLOSE LS.
           OPEN I-O LS.
           READ LS.
           MOVE "Z€ZZ" TO LR.
           REWRITE LR.
           DISPLAY "LS-REWR=" FL.
           CLOSE LS.
           OPEN INPUT LS.
           READ LS.
           DISPLAY "LS-AGAIN=" FL " " FUNCTION ORD(LR(2:1)).
           CLOSE LS.
           OPEN OUTPUT NL.
           MOVE N"€A" TO NR.
           WRITE NR.
           DISPLAY "NL-EURO=" FN.
           CLOSE NL.
           OPEN INPUT NL.
           MOVE SPACES TO NR.
           READ NL.
           DISPLAY "NL-READ=" FN " " NR.
           CLOSE NL.
           STOP RUN.
