      *>--- conformance-case
      *> id: l1c05_coded_character_values
      *> rules: [DOC-A.1-32]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.1.2 (A.1 item 32) — the alphanumeric coded character
      *>   values of COBOL items
      *> Rule (A.1 item 32): "Computer's coded character set (coded
      *>   character values for certain COBOL items). This item is
      *>   required. This item shall be documented in the implementor's
      *>   user documentation."
      *> cite.py --check 8.1.2 "The implementor shall specify one and
      *>   only one alphanumeric coded character value" -> OK  §8.1.2 3)
      *>   (Computer's coded character set)
      *> cite.py --check 15.70.4 "the returned value is the ordinal
      *>   position of argument-1 in the current alphanumeric program
      *>   collating sequence" -> OK  §15.70.4 1)  (Returned value
      *>   rules)
      *> The documented choice pinned (docs/CONFORMANCE.md DOC-A.1-32):
      *>   SPACE U+0020, QUOTE U+0022, ZERO U+0030, space fill U+0020, B
      *>   U+0020, 0 U+0030, digits U+0030-U+0039, C R U+0043 U+0052, D
      *>   B U+0044 U+0042, - U+002D, + U+002B, * U+002A, / U+002F,
      *>   comma U+002C, period U+002E, default currency $ U+0024. They
      *>   are observed through FUNCTION ORD, which with no PROGRAM
      *>   COLLATING SEQUENCE is the code unit plus one
      *>   (docs/CONFORMANCE.md DOC-A.1-8).
      *> Derivation, ORD = code + 1 (decimal):
      *>   SPACE 20h 033; QUOTE 22h 035; ZERO 30h 049; fill 20h 033
      *>   9B9 of 12 = "1 2": B 033; 909 of 19 = "109": 0 049, 9 39h 058
      *>   Z9 of 5 = " 5": the suppressed digit space 033
      *>   99CR of -12 = "12CR": C 43h 068, R 52h 083
      *>   99DB of -12 = "12DB": D 44h 069, B 42h 067
      *>   -99 of -5 = "-05": 2Dh 046; +99 of 5 = "+05": 2Bh 044
      *>   *99 of 5 = "*05": 2Ah 043; 9/9 of 12 = "1/2": 2Fh 048
      *>   9,999 of 1234: comma 2Ch 045; 9.9 of 1.5: period 2Eh 047
      *>   $99 of 5 = "$05": 24h 037
      *> SPACE, the space fill, B and the Z space are one value (033);
      *>   ZERO, the 0 insertion and the digit 0 are one value (049), as
      *>   §8.1.2 requires.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C05N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-C    PIC X.
       01 W-L    PIC X(6).
       01 W-O    PIC 9(3).
       01 W-FILL PIC X(4).
       01 W-NEG  PIC S99 VALUE -12.
       01 W-M5   PIC S9  VALUE -5.
       01 E-B    PIC 9B9.
       01 E-Z    PIC 909.
       01 E-SUP  PIC Z9.
       01 E-CR   PIC 99CR.
       01 E-DB   PIC 99DB.
       01 E-MIN  PIC -99.
       01 E-PLS  PIC +99.
       01 E-AST  PIC *99.
       01 E-SL   PIC 9/9.
       01 E-CM   PIC 9,999.
       01 E-PT   PIC 9.9.
       01 E-CUR  PIC $99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACE TO W-C.  MOVE "SPACE" TO W-L. PERFORM SHOW.
           MOVE QUOTE TO W-C.  MOVE "QUOTE" TO W-L. PERFORM SHOW.
           MOVE ZERO TO W-C.   MOVE "ZERO" TO W-L.  PERFORM SHOW.
           MOVE "A" TO W-FILL.
           MOVE W-FILL(2:1) TO W-C. MOVE "FILL" TO W-L. PERFORM SHOW.
           MOVE 12 TO E-B.
           MOVE E-B(2:1) TO W-C. MOVE "B" TO W-L. PERFORM SHOW.
           MOVE 19 TO E-Z.
           MOVE E-Z(2:1) TO W-C. MOVE "0" TO W-L. PERFORM SHOW.
           MOVE E-Z(3:1) TO W-C. MOVE "DIGIT9" TO W-L. PERFORM SHOW.
           MOVE 5 TO E-SUP.
           MOVE E-SUP(1:1) TO W-C. MOVE "ZSPACE" TO W-L. PERFORM SHOW.
           MOVE W-NEG TO E-CR.
           MOVE E-CR(3:1) TO W-C. MOVE "C" TO W-L. PERFORM SHOW.
           MOVE E-CR(4:1) TO W-C. MOVE "R" TO W-L. PERFORM SHOW.
           MOVE W-NEG TO E-DB.
           MOVE E-DB(3:1) TO W-C. MOVE "D" TO W-L. PERFORM SHOW.
           MOVE E-DB(4:1) TO W-C. MOVE "DB-B" TO W-L. PERFORM SHOW.
           MOVE W-M5 TO E-MIN.
           MOVE E-MIN(1:1) TO W-C. MOVE "MINUS" TO W-L. PERFORM SHOW.
           MOVE 5 TO E-PLS.
           MOVE E-PLS(1:1) TO W-C. MOVE "PLUS" TO W-L. PERFORM SHOW.
           MOVE 5 TO E-AST.
           MOVE E-AST(1:1) TO W-C. MOVE "STAR" TO W-L. PERFORM SHOW.
           MOVE 12 TO E-SL.
           MOVE E-SL(2:1) TO W-C. MOVE "SLASH" TO W-L. PERFORM SHOW.
           MOVE 1234 TO E-CM.
           MOVE E-CM(2:1) TO W-C. MOVE "COMMA" TO W-L. PERFORM SHOW.
           MOVE 1.5 TO E-PT.
           MOVE E-PT(2:1) TO W-C. MOVE "POINT" TO W-L. PERFORM SHOW.
           MOVE 5 TO E-CUR.
           MOVE E-CUR(1:1) TO W-C. MOVE "DOLLAR" TO W-L. PERFORM SHOW.
           STOP RUN.
       SHOW.
           MOVE FUNCTION ORD(W-C) TO W-O.
           DISPLAY W-L "=" W-O.
