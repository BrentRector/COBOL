*>--- conformance-case
*> id: pb576_file_section_value_initialize
*> rules: [GR-13.18.63.4-1, GR-13.18.63.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB576 — ISO 13.18.63.4 GR2, "In the file section, VALUE clauses take effect only during the
      *> execution of an INITIALIZE statement. The initial value of the data items in the file section is
      *> undefined." No spec-derived test pinned the operative half, and for a record item whose usage is
      *> float-short / float-long / float-extended the ONE path the rule names could not execute at all until
      *> kb/Work PB420 (commit c2f0c4c50) removed the INITIALIZE emitter's float-receiver loud - so GR2's whole
      *> float population was undelivered. Every expected value below is derived from the rules, not measured:
      *>   L1 the operative half over all three categories, including the float member the refutation named.
      *>      R1/R2 take their literals; R3 is compared against a MOVE of the SAME literal into the SAME usage,
      *>      because GR1 gives it "an approximation of the arithmetic value of the literal" and 14.9.25 gives the
      *>      MOVE the same approximation - so SAME is the rule's own assertion and no float rendering is pinned.
      *>   L2 the boundary: a PLAIN INITIALIZE does not apply the VALUE clause at all. 14.9.20.4 GR5c4 qualifies
      *>      every elementary item ("Neither the REPLACING phrase nor the VALUE phrase is specified") and GR6c's
      *>      fill table supplies the CATEGORY DEFAULT - alphanumeric SPACES, numeric ZEROES, and ZEROES for the
      *>      float member, which is category numeric by 8.5.2. The VALUE clause is not what is applied here, and
      *>      NE for R3 against WS-C is what proves it.
      *> ⛔ GR2's SECOND sentence is why nothing here asserts the record area's content BEFORE the INITIALIZE:
      *> "The initial value of the data items in the file section is undefined", so no conforming program can
      *> depend on it and no golden may pin it.
      *> FLOAT-LONG is COBOL-2002 (13.18.60.2); the edition floor for the TO VALUE vehicle is already witnessed by
      *> conformance:cases/14.9.20-INITIALIZE/pb420-initialize-float-to-value-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB576FSV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO "pb576_fsv.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD OUTF.
       01 R.
          05 R1 PIC X(3) VALUE "ABC".
          05 R2 PIC 9(3) VALUE 123.
          05 R3 FLOAT-LONG VALUE 0.1.
       WORKING-STORAGE SECTION.
       01 WS-C FLOAT-LONG.
       PROCEDURE DIVISION.
           MOVE 0.1 TO WS-C
           OPEN OUTPUT OUTF
           MOVE "XYZ" TO R1
           MOVE 999 TO R2
           MOVE 9.0 TO R3
           INITIALIZE R ALL TO VALUE
           IF R3 = WS-C
               DISPLAY "L1 TOVAL R1=[" R1 "] R2=[" R2 "] R3=SAME"
           ELSE
               DISPLAY "L1 TOVAL R1=[" R1 "] R2=[" R2 "] R3=NE"
           END-IF
           INITIALIZE R
           IF R3 = WS-C
               DISPLAY "L2 PLAIN R1=[" R1 "] R2=[" R2 "] R3=SAME"
           ELSE
               DISPLAY "L2 PLAIN R1=[" R1 "] R2=[" R2 "] R3=NE"
           END-IF
           CLOSE OUTF
           STOP RUN.
