       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB690LSQ.
      *> ISO/IEC 1989:2023 §12.4.5.10.3 GR2 ("The range of allowable
      *> characters in a line sequential file is implementor-defined")
      *> with §8.1.2 (the encoding is the implementor's): a LINE
      *> SEQUENTIAL file with no CODE-SET clause is UTF-8 TEXT (owner
      *> decision kb/Work R51, which supersedes R47 for this
      *> organization; kb/Work PB1760; docs/CONFORMANCE.md DOC-A.1-31
      *> and DOC-A.1-115). Every character a record area can hold
      *> round-trips; what stays OUTSIDE the set is the C0 controls and
      *> a lone surrogate, which is not a character and has no UTF-8
      *> form - §14.9.51.4 GR23: "the execution of the WRITE statement
      *> is unsuccessful and the I-O status ... is set to '71'".
      *>
      *> §14.9.35.4 GR17 b) compares BYTES - "If the number of bytes in
      *> ... the record referenced by record-name-1 is greater than the
      *> number of bytes in the record being replaced, the execution of
      *> the REWRITE statement is unsuccessful" ('44'). A line is
      *> replaced in place, so the bytes are the record's UTF-8 bytes on
      *> the medium: "Z€ZZ" (6) replaces "A€BC" (6); "€€€€" (12) cannot
      *> replace "AéBC" (5).
      *>
      *> Why each leg can fail:
      *>  LS-EURO  - '00': U+20AC is a member (it was '71' under R47).
      *>  LS-LONE  - '71': U+D800 alone is outside the set.
      *>  LS-READ* - the two lines come back whole: ORD 8365 (U+20AC)
      *>             and ORD 234 (U+00E9), then at end.
      *>  LS-REWR* - '00' for the equal byte length, '44' for the
      *>             longer one; LS-AGAIN* shows the file.
      *>  NL-*     - a national record area holding U+20AC is written
      *>             and read back whole: '00' and the character itself.
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
           MOVE FUNCTION CHAR(55297) TO LR.
           WRITE LR.
           DISPLAY "LS-LONE=" FL.
           CLOSE LS.
           OPEN INPUT LS.
           READ LS.
           DISPLAY "LS-READ=" FL " " FUNCTION ORD(LR(2:1)).
           READ LS.
           DISPLAY "LS-READ2=" FL " " FUNCTION ORD(LR(2:1)).
           READ LS AT END DISPLAY "LS-READ3 AT END".
           CLOSE LS.
           OPEN I-O LS.
           READ LS.
           MOVE "Z€ZZ" TO LR.
           REWRITE LR.
           DISPLAY "LS-REWR=" FL.
           READ LS.
           MOVE "€€€€" TO LR.
           REWRITE LR.
           DISPLAY "LS-REWR2=" FL.
           CLOSE LS.
           OPEN INPUT LS.
           READ LS.
           DISPLAY "LS-AGAIN=" FL " " LR.
           READ LS.
           DISPLAY "LS-AGAIN2=" FL " " LR.
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
