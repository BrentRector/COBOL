*>--- conformance-case
*> id: pb58_argument_predicates_legal
*> rules: [AR-15.3-2, AR-15.3-6, AR-15.9.3-3, AR-15.18.3-1, AR-15.18.3-2, AR-15.18.3-3, AR-15.22.3-1, AR-15.23.3-1, AR-15.23.3-2, AR-15.24.3-1, AR-15.37.3-2, AR-15.37.3-3, AR-15.48.3-3, AR-15.57.3-1, AR-15.59.3-2, RV-15.59.4-3, AR-15.64.3-1, AR-15.67.3-1, AR-15.69.3-1, AR-15.70.3-1, AR-15.71.3-1, AR-15.71.3-3, AR-15.72.3-1, AR-15.72.3-3, AR-15.78.3-1, AR-15.79.3-1, AR-15.87.3-3, RV-15.87.4-1, RV-15.88.4-1, AR-15.93.3-1, AR-15.97.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB58 - the argument screen's new predicate kinds must never OVER-reject: every shape below is legal
      *> under its function's 15.x.3 argument rule and compiles and runs. ORD of a one-character item and a
      *> one-character reference-modified view (15.70.3 r1); LOWER-CASE/REVERSE of non-empty operands
      *> (15.57.3/15.78.3 r1); TRIM with a single-character argument-2 (15.96.3 r2); FIND-STRING with an
      *> integer DATA ITEM and an integer LITERAL as argument-3 (15.37.3 r3); MAX over an ordinary (not
      *> strongly-typed) group (15.59.3 r1); ORD-MAX/ORD-MIN over same-class lists (15.71.3/15.72.3 r3);
      *> ANNUITY with an integer argument-2 (15.9.3 r3); MOD with integer literals; the date family with
      *> integer arguments (15.22.3/15.24.3/15.23.3); NUMVAL/NUMVAL-F/TEST-NUMVAL over string literals
      *> (15.67.3/15.69.3/15.93.3 r1); SUM over a numeric list (15.88.3 r1); CONCAT over display items, an
      *> unsigned integer numeric item and an unsigned integer literal (15.18.3 r1/r2/r3) and over an
      *> all-national list; INTEGER-OF-FORMATTED-DATE with a data-item argument-2 of the format's class
      *> (15.48.3 r3); SECONDS-FROM-FORMATTED-TIME with ALL "hh:mm:ss" as the format literal (8.3.3.6.3 SR1
      *> - a figurative constant is admitted where 'literal' appears; it was COBOLNET1517); SUBSTITUTE
      *> with an EMPTY argument-3 (a deletion - only argument-1/-2 carry 15.87.3 r3's zero-length rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB58PREDLEGAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3    PIC X(3) VALUE "ABC".
       01 X1    PIC X VALUE "B".
       01 N9    PIC 9(3) VALUE 5.
       01 I2    PIC 9(2) VALUE 2.
       01 NAT   PIC N(3) VALUE N"ABC".
       01 R     PIC S9(9)V99.
       01 E     PIC -(9)9.99.
       01 T     PIC 9(4).
       01 A20   PIC X(20).
       01 NA20  PIC N(20).
       01 D8    PIC X(8) VALUE "20240102".
       01 TM8   PIC X(8) VALUE "01:02:03".
       01 GRP.
          05 G1 PIC X(2) VALUE "AB".
          05 G2 PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           COMPUTE T = FUNCTION ORD(X1).
           DISPLAY "ORD=" T.
           COMPUTE T = FUNCTION ORD(X3(2:1)).
           DISPLAY "ORDRM=" T.
           MOVE FUNCTION LOWER-CASE(X3(1:1)) TO A20.
           DISPLAY "LOW=" A20.
           MOVE FUNCTION REVERSE(X3) TO A20.
           DISPLAY "REV=" A20.
           MOVE FUNCTION TRIM(X3 "C") TO A20.
           DISPLAY "TRIM=" A20.
           COMPUTE T = FUNCTION FIND-STRING(X3 "B" I2).
           DISPLAY "FIND=" T.
           COMPUTE T = FUNCTION FIND-STRING(X3 "C" 0).
           DISPLAY "FIND0=" T.
           MOVE FUNCTION MAX(GRP "AAAA") TO A20.
           DISPLAY "MAXG=" A20.
           COMPUTE T = FUNCTION ORD-MAX(X3 "ZZ" X1).
           DISPLAY "ORDMAX=" T.
           COMPUTE T = FUNCTION ORD-MIN(N9 I2 7).
           DISPLAY "ORDMIN=" T.
           COMPUTE R = FUNCTION ANNUITY(0 I2).
           MOVE R TO E.
           DISPLAY "ANN=" E.
           COMPUTE R = FUNCTION MOD(7 2).
           MOVE R TO E.
           DISPLAY "MOD=" E.
           COMPUTE T = FUNCTION DATE-OF-INTEGER(N9).
           DISPLAY "DOI=" T.
           COMPUTE T = FUNCTION DAY-OF-INTEGER(N9).
           DISPLAY "DAYOI=" T.
           COMPUTE T = FUNCTION DATE-TO-YYYYMMDD(240102 I2 2000).
           DISPLAY "DTY=" T.
           COMPUTE R = FUNCTION NUMVAL(" 12.5 ").
           MOVE R TO E.
           DISPLAY "NV=" E.
           COMPUTE R = FUNCTION NUMVAL-F(" 1.5E+1 ").
           MOVE R TO E.
           DISPLAY "NVF=" E.
           COMPUTE T = FUNCTION TEST-NUMVAL("12.5").
           DISPLAY "TNV=" T.
           COMPUTE R = FUNCTION SUM(N9 I2 3.5).
           MOVE R TO E.
           DISPLAY "SUM=" E.
           MOVE FUNCTION CONCAT("A" N9 "-" 12 X3) TO A20.
           DISPLAY "CAT=[" A20 "]".
           MOVE FUNCTION CONCAT(NAT N"X") TO NA20.
           DISPLAY "CATN=[" FUNCTION DISPLAY-OF(NA20) "]".
           COMPUTE T = FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" D8).
           DISPLAY "IOFD=" T.
           COMPUTE R = FUNCTION SECONDS-FROM-FORMATTED-TIME(ALL "hh:mm:ss" TM8).
           MOVE R TO E.
           DISPLAY "SFFTALL=" E.
           MOVE FUNCTION SUBSTITUTE(X3 "A" "" "C" "ZZ") TO A20.
           DISPLAY "SUBST=[" A20 "]".
           STOP RUN.
       END PROGRAM PB58PREDLEGAL.
