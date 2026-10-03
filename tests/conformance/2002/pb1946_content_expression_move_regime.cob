      *> kb/Work PB1946 (verdict kb/Work PB1936) - ISO 14.8.2.3.3 2) d): a
      *> BY CONTENT argument whose formal is not numeric conforms "as for a
      *> MOVE statement with the argument as the sending operand", and an
      *> arithmetic-expression argument is a sending operand (14.9.4.3 SR17),
      *> so its numeric VALUE is the sender, judged by Table 16 exactly as a
      *> numeric literal is. A numeric value moves into a numeric-edited
      *> receiver (14.9.25.3 Table 16, Numeric -> "Numeric, Numeric-edited":
      *> Yes, integer and noninteger alike) and is edited into the mask
      *> (14.9.25.4 GR6). Both lanes ask it, the CALL ... AS NESTED lane and
      *> the typed INVOKE lane, through OoConformance.ContentValueMismatch.
      *> Before the fix every one of these was refused as COBOLNET1688 or
      *> COBOLNET0828 on the reading that MOVE takes only an identifier or a
      *> literal.
      *> EXPECTED VALUES, DERIVED (N3 = 123, N4 = 3):
      *>  E1  N3 * 2 = 246 into PIC ZZ9.99                  => [246.00]
      *>  E2  N3 / 7 = 17.5714... into PIC ZZ9.99, the low-order digits
      *>      truncated by the MOVE's alignment on the decimal point
      *>                                                    => [ 17.57]
      *>  S1  N4 - 10 = -7 into PIC -ZZ9, the fixed minus sign position
      *>      takes the sign, ZZ9 suppresses leading zeros   => [-  7]
      *>  U1  N4 - 10 = -7 into the unsigned PIC ZZ9: 14.9.25.4 GR6 d) 2) b)
      *>      "the absolute value of the sending value is used"
      *>                                                    => [  7]
      *>  K1  N3 * 100 = 12300 into PIC $$$,$$9.99          => [$12,300.00]
      *>  F1  FUNCTION MAX (N3 N4): 15.59.1 "All arguments integer" makes it
      *>      an Integer function, so Table 16's Integer row -> alphanumeric
      *>      is Yes, the digits left-justified and space-filled
      *>                                                    => [123 ]
      *>      (the CALL lane used to ask the expression's noninteger row of
      *>      the function and refuse it; the INVOKE lane read the function's
      *>      own type)
      *> Each line is printed by the CALL lane (C:) and then the INVOKE lane (I:).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1946K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1946K.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:A=[" L "]"
           GOBACK.
       END METHOD MA.
       IDENTIFICATION DIVISION.
       METHOD-ID. MS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC -ZZ9.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:S=[" L "]"
           GOBACK.
       END METHOD MS.
       IDENTIFICATION DIVISION.
       METHOD-ID. MU.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:U=[" L "]"
           GOBACK.
       END METHOD MU.
       IDENTIFICATION DIVISION.
       METHOD-ID. MK.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC $$$,$$9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:K=[" L "]"
           GOBACK.
       END METHOD MK.
       IDENTIFICATION DIVISION.
       METHOD-ID. MF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "I:F=[" L "]"
           GOBACK.
       END METHOD MF.
       END OBJECT.
       END CLASS PB1946K.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1946K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1946K.
       01 N3 PIC 9(3) VALUE 123.
       01 N4 PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1946K "NEW" RETURNING O
           CALL "PB1946A" AS NESTED USING BY CONTENT N3 * 2
           INVOKE O "MA" USING BY CONTENT N3 * 2
           CALL "PB1946A" AS NESTED USING BY CONTENT N3 / 7
           INVOKE O "MA" USING BY CONTENT N3 / 7
           CALL "PB1946S" AS NESTED USING BY CONTENT N4 - 10
           INVOKE O "MS" USING BY CONTENT N4 - 10
           CALL "PB1946U" AS NESTED USING BY CONTENT N4 - 10
           INVOKE O "MU" USING BY CONTENT N4 - 10
           CALL "PB1946D" AS NESTED USING BY CONTENT N3 * 100
           INVOKE O "MK" USING BY CONTENT N3 * 100
           CALL "PB1946F" AS NESTED
               USING BY CONTENT FUNCTION MAX (N3 N4)
           INVOKE O "MF" USING BY CONTENT FUNCTION MAX (N3 N4)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:A=[" L "]"
           GOBACK.
       END PROGRAM PB1946A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC -ZZ9.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:S=[" L "]"
           GOBACK.
       END PROGRAM PB1946S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946U.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:U=[" L "]"
           GOBACK.
       END PROGRAM PB1946U.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946D.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC $$$,$$9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:K=[" L "]"
           GOBACK.
       END PROGRAM PB1946D.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:F=[" L "]"
           GOBACK.
       END PROGRAM PB1946F.
       END PROGRAM PB1946R.
