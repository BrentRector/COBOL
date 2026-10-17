*>--- conformance-case
*> id: pb339_into_current_record_2014
*> rules: [GR-13.18.32.4-1, GR-13.18.32.4-2, GR-13.18.43.4-16, GR-14.9.30.4-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB339 — the sending operand of a READ … INTO / RETURN … INTO
      *> implicit MOVE is THE CURRENT RECORD, not the padded record area.
      *> The 2014 leg.
      *>
      *> ISO §14.9.30.4 GR4 b) (and its RETURN twin §14.9.34.4 GR5 b), word
      *> for word bar one preposition): "The current record is moved from the
      *> record area to the area specified by identifier-1 according to the
      *> rules for the MOVE statement without the CORRESPONDING phrase.  The
      *> size of the current record is determined by rules specified in the
      *> RECORD clause.  If the file description entry contains a RECORD IS
      *> VARYING clause, the implied move is an alphanumeric group move."
      *>
      *> §13.18.43.4 GR16 supplies that size for a FORMAT 2 RECORD clause:
      *> "If the INTO phrase is specified in the READ or RETURN statement,
      *> the number of bytes in the current record that participate as the
      *> sending operands in the implicit MOVE statement is determined by the
      *> following conditions:  a) If data-name-1 is specified, by the content
      *> of the data item referenced by data-name-1.  b) If data-name-1 is not
      *> specified, by the value that would have been moved into the data item
      *> referenced by data-name-1 had data-name-1 been specified."
      *> GR15 has just put the just-read length there, so both arms name the
      *> record's own byte count — never the area's width.
      *>
      *> A LEFT-justified receiver cannot tell the two apart (both space-fill
      *> on the right).  The discriminators below are the JUSTIFIED clause,
      *> whose truncation takes the LEFTMOST characters, and the group-move
      *> designation, which forbids editing.
      *>
      *> EXPECTED VALUES, each derived before it was run:
      *>  A  F holds the 5-byte record "ABCDE" (WS-LEN = 5 at the WRITE, so
      *>     §13.18.43.4 GR13 a) wrote five bytes).  GR15 restores WS-LEN = 5,
      *>     GR16 a) makes the sender those five bytes.  Receiver X(10)
      *>     JUSTIFIED: §13.18.32.4 GR2 — "the data is aligned at the rightmost
      *>     character position … with … space fill for the leftmost character
      *>     positions" => "     ABCDE".  Sending the 20-byte area instead
      *>     yields ten spaces (that is the defect).
      *>  B  Receiver X(3) JUSTIFIED, sender five bytes: §13.18.32.4 GR1 —
      *>     "the leftmost character positions … of the sending operand shall
      *>     be truncated" => "CDE".  From the padded area it would be three
      *>     spaces.
      *>  C  Receiver X(10) plain: §14.6.8.5 left-aligned, right space fill =>
      *>     "ABCDE     ".  The control leg: identical either way.
      *>  D  Receiver PIC XXBXXBXX (eight character positions).  GR4 b) makes
      *>     the move an alphanumeric GROUP move, and §14.9.25.4 GR4 treats a
      *>     group move "exactly as if it were an alphanumeric to alphanumeric
      *>     elementary move, except that there is no conversion of data from
      *>     one form of internal representation to another", the receiving
      *>     area "filled without consideration for the individual elementary
      *>     or group items" — so NO editing: "ABCDE   ".  Editing the sender
      *>     would give "AB CD E " (or, from the padded area, "AB CD E ").
      *>  E  Receiver X(3) plain: §14.6.8.5 right truncation => "ABC".
      *>  F  GR16 b), the arm with no DEPENDING ON phrase: G has two record
      *>     descriptions and WRITE G-SHORT wrote its own five bytes
      *>     (§13.18.43.4 GR13 b).  The sender is still five bytes, so the
      *>     X(10) JUSTIFIED receiver gets "     ABCDE".
      *>  G  RETURN … INTO, §14.9.34.4 GR5 b) + §13.18.43.4 GR16 a): the SD
      *>     released eight bytes "K01PQRST"; receiver X(12) JUSTIFIED =>
      *>     "    K01PQRST".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB339C3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb339c3f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS F-ST.
           SELECT G ASSIGN TO "pb339c3g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS G-ST.
           SELECT SRTF ASSIGN TO "pb339c3s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD IS VARYING IN SIZE FROM 1 TO 20
              DEPENDING ON WS-LEN.
       01 F-REC PIC X(20).
       FD G RECORD IS VARYING IN SIZE FROM 3 TO 20.
       01 G-LONG PIC X(20).
       01 G-SHORT PIC X(5).
       SD SRTF RECORD IS VARYING IN SIZE FROM 3 TO 20
              DEPENDING ON WS-SLEN.
       01 S-REC.
          05 S-KEY PIC X(3).
          05 S-DATA PIC X(17).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 G-ST PIC XX.
       01 WS-LEN PIC 9(4) VALUE 5.
       01 WS-SLEN PIC 9(4) VALUE 8.
       01 WS-J10 PIC X(10) JUSTIFIED RIGHT.
       01 WS-J3 PIC X(3) JUSTIFIED RIGHT.
       01 WS-P10 PIC X(10).
       01 WS-P3 PIC X(3).
       01 WS-ED PIC XXBXXBXX.
       01 WS-J12 PIC X(12) JUSTIFIED RIGHT.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           MOVE 5 TO WS-LEN.
           OPEN OUTPUT F.
           MOVE "ABCDE" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           PERFORM LEG-A.
           PERFORM LEG-B.
           PERFORM LEG-C.
           PERFORM LEG-D.
           PERFORM LEG-E.
           PERFORM LEG-F.
           SORT SRTF ON ASCENDING KEY S-KEY
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "DONE".
           STOP RUN.
       LEG-A.
           MOVE 0 TO WS-LEN.
           MOVE ALL "." TO WS-J10.
           OPEN INPUT F.
           READ F INTO WS-J10 AT END DISPLAY "A-ATEND" END-READ.
           CLOSE F.
           DISPLAY "LEN=" WS-LEN.
           DISPLAY "A=[" WS-J10 "]".
       LEG-B.
           MOVE ALL "." TO WS-J3.
           OPEN INPUT F.
           READ F INTO WS-J3 AT END DISPLAY "B-ATEND" END-READ.
           CLOSE F.
           DISPLAY "B=[" WS-J3 "]".
       LEG-C.
           MOVE ALL "." TO WS-P10.
           OPEN INPUT F.
           READ F INTO WS-P10 AT END DISPLAY "C-ATEND" END-READ.
           CLOSE F.
           DISPLAY "C=[" WS-P10 "]".
       LEG-D.
           MOVE ALL "." TO WS-ED.
           OPEN INPUT F.
           READ F INTO WS-ED AT END DISPLAY "D-ATEND" END-READ.
           CLOSE F.
           DISPLAY "D=[" WS-ED "]".
       LEG-E.
           MOVE ALL "." TO WS-P3.
           OPEN INPUT F.
           READ F INTO WS-P3 AT END DISPLAY "E-ATEND" END-READ.
           CLOSE F.
           DISPLAY "E=[" WS-P3 "]".
       LEG-F.
           OPEN OUTPUT G.
           MOVE "ABCDE" TO G-SHORT.
           WRITE G-SHORT.
           CLOSE G.
           MOVE ALL "." TO WS-J10.
           OPEN INPUT G.
           READ G INTO WS-J10 AT END DISPLAY "F-ATEND" END-READ.
           CLOSE G.
           DISPLAY "F=[" WS-J10 "]".
       IN-P SECTION.
       IN-P1.
           MOVE 8 TO WS-SLEN.
           MOVE "K01" TO S-KEY.
           MOVE "PQRST" TO S-DATA.
           RELEASE S-REC.
       OUT-P SECTION.
       OUT-P1.
           MOVE 0 TO WS-SLEN.
           MOVE ALL "." TO WS-J12.
           RETURN SRTF INTO WS-J12
               AT END DISPLAY "G-ATEND"
           END-RETURN.
           DISPLAY "SLEN=" WS-SLEN.
           DISPLAY "G=[" WS-J12 "]".
