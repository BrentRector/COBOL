       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1759HV.
      *> kb/Work PB1759 / PB1093 (owner decision R52) - HIGH-VALUE IS
      *> THE HIGHEST CHARACTER OF THE NATIVE SEQUENCE, WHOSE 65,536
      *> CHARACTERS ARE THE UTF-16 CODE UNITS (DOC-A.1-31/-188):
      *>  §8.3.3.6.4 GR6 - "the high-value format represents the
      *>  character ... that has the highest ordinal position in the
      *>  runtime collating sequence" - U+FFFF, ORDINAL 65536 (§15.70
      *>  ORD: "The lowest ordinal position is 1").
      *> WHY EACH LEG CAN FAIL:
      *>  ORD-HV  - 65536; U+00FF (THE OLD PIN) WAS 256.
      *>  WIDE    - CHAR(20001) = U+4E20 IS BELOW HIGH-VALUE; THE OLD
      *>            PIN PUT EVERY CHARACTER ABOVE U+00FF ABOVE IT.
      *>  NAT     - THE NATIONAL HIGH-VALUE IS U+FFFF TOO (NX"FFFF").
      *>  XFF     - THE LITERAL X"FF" IS U+00FF IN MEMORY, NOT
      *>            HIGH-VALUE (OWNER R52).
      *>  COMP    - A GROUP MOVE OF HIGH-VALUES OVER A COMP-5 FIELD
      *>            KEEPS ITS CHARACTERS: THE FIELD HOLDS X'FFFF' (-1)
      *>            AND THE GROUP STILL EQUALS HIGH-VALUES.
      *>  CLASS   - IN SPECIAL-NAMES HIGH-VALUE IS THE NATIVE EXTREME
      *>            (§12.3.7.4 GR10): CLASS HVC IS HIGH-VALUE HOLDS
      *>            CHAR(65536) = U+FFFF AND NOT CHAR(256) = U+00FF.
      *>  YUML    - THE ACCEPTED COST (DOC-A.1-31): A REAL U+00FF THAT
      *>            PASSES THROUGH A ONE-BYTE RECORD MEDIUM SHARES THE
      *>            BYTE X'FF' AND READS BACK AS HIGH-VALUE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HVC IS HIGH-VALUE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ ASSIGN TO "pb1759hv-s.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD SQ.
       01 SREC PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 XA PIC X.
       01 NA PIC N.
       01 G.
          05 GA PIC X(2).
          05 GB PIC S9(4) COMP-5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE HIGH-VALUE TO XA.
           DISPLAY "ORD-HV=" FUNCTION ORD(XA).
           MOVE FUNCTION CHAR(20001) TO XA.
           IF XA < HIGH-VALUE DISPLAY "WIDE=LT" ELSE DISPLAY "WIDE=GE"
           END-IF.
           MOVE HIGH-VALUE TO NA.
           IF NA = NX"FFFF" DISPLAY "NAT=FFFF" ELSE DISPLAY "NAT=OTHER"
           END-IF.
           MOVE X"FF" TO XA.
           IF XA = HIGH-VALUE DISPLAY "XFF=HV" ELSE DISPLAY "XFF=NOT-HV"
           END-IF.
           DISPLAY "XFF-ORD=" FUNCTION ORD(XA).
           MOVE HIGH-VALUES TO G.
           DISPLAY "COMP=" GB.
           IF G = HIGH-VALUES DISPLAY "COMP-G=HV"
           ELSE DISPLAY "COMP-G=NOT-HV" END-IF.
           MOVE FUNCTION CHAR(65536) TO XA.
           IF XA IS HVC DISPLAY "CLASS-FFFF=IN"
           ELSE DISPLAY "CLASS-FFFF=OUT" END-IF.
           MOVE FUNCTION CHAR(256) TO XA.
           IF XA IS HVC DISPLAY "CLASS-00FF=IN"
           ELSE DISPLAY "CLASS-00FF=OUT" END-IF.
           OPEN OUTPUT SQ.
           MOVE X"FF" TO SREC.
           MOVE HIGH-VALUE TO SREC(2:1).
           WRITE SREC.
           CLOSE SQ.
           OPEN INPUT SQ.
           READ SQ.
           IF SREC(1:1) = HIGH-VALUE DISPLAY "YUML=HV " FS
           ELSE DISPLAY "YUML=NOT-HV " FS END-IF.
           IF SREC(2:1) = HIGH-VALUE DISPLAY "HV=HV"
           ELSE DISPLAY "HV=NO"
           END-IF.
           CLOSE SQ.
           STOP RUN.
