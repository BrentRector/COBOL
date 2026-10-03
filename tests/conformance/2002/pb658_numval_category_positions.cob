      *> kb/Work PB658 -- the admit side of the CATEGORY-worded string arguments.
      *> NUMVAL (ISO 15.67.3 r1, "an alphanumeric or national ... data item", category by 8.5.2.1's closing
      *> sentence) admits an item of category ALPHANUMERIC: a PIC X item, an alphanumeric GROUP (8.5.2.3 3)), and a
      *> REFERENCE-MODIFIED view of a numeric-edited item, which 8.4.3.3.4 GR6 makes an elementary item of category
      *> alphanumeric whatever the underlying item was. TEST-NUMVAL (15.93.3 r1) is CLASS-worded, "a data item of
      *> class alphanumeric or national", so Table 2's class alphanumeric admits the numeric-edited item itself.
      *> ED holds " 25"; GRP holds " 7   "; WX holds "  12  ". The expected NUMVAL values are those digits.
      *> The reject side is conformance:negative/pb658-numval-numeric-edited-argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB658POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED  PIC ZZ9.
       01 GRP.
          05 G1 PIC X(3) VALUE " 7 ".
          05 G2 PIC X(3) VALUE SPACES.
       01 WX  PIC X(6) VALUE "  12  ".
       01 R   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 25 TO ED
           COMPUTE R = FUNCTION NUMVAL(ED(1:3))
           DISPLAY "REFMOD " R
           COMPUTE R = FUNCTION TEST-NUMVAL(ED)
           DISPLAY "TEST " R
           COMPUTE R = FUNCTION NUMVAL(GRP)
           DISPLAY "GRP " R
           COMPUTE R = FUNCTION NUMVAL(WX)
           DISPLAY "PICX " R
           STOP RUN.
       END PROGRAM PB658POS.
