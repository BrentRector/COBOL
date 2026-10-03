      *> kb/Work PB1113 - ISO 14.8.2.3.3 2): for a NESTED program and
      *> for a method, a BY CONTENT argument conforms by the rule its
      *> FORMAL selects - a) numeric: "the same as for a COMPUTE
      *> statement"; d) otherwise: "the same as for a MOVE statement with
      *> the argument as the sending operand". A literal or an expression
      *> is DESCRIBED as the sender it is and asked that question, on the
      *> CALL ... AS NESTED lane and the typed INVOKE lane alike (one
      *> verdict, OoConformance.ContentValueMismatch). Before the fix the
      *> literal/boolean lanes carried hand lists and refused every pair
      *> below except B"1" -> N(4) on CALL.
      *> EXPECTED VALUES, DERIVED (14.9.25.3 Table 16; 14.9.25.4 GR4/GR6):
      *>  E1  123 into PIC ZZ9.99: Numeric-integer -> numeric-edited Yes,
      *>      edited into the mask                         => [123.00]
      *>  E2  12.5 into PIC ZZ9.99: Numeric-noninteger -> numeric-edited
      *>      Yes                                          => [ 12.50]
      *>  N1  12 into PIC N(4): Numeric-integer -> national Yes, national
      *>      space fill                                   => [12  ]
      *>  N2  B"1" into PIC N(4): Boolean -> national Yes   => [1   ]
      *>  N3  B1 B-AND B2 (1100 AND 1010) into PIC N(4)    => [1000]
      *>  X1  -5 into PIC X(4): Numeric-integer -> alphanumeric Yes, and
      *>      GR6 "the operational sign is not moved"      => [5   ]
      *>  G1  B"10" into a 4-character alphanumeric group: a group move
      *>      (GR4), the characters space-filled            => [10  ]
      *>  G2  -12 into the same group: GR4 "as if it were an alphanumeric
      *>      to alphanumeric elementary move ... no conversion" - the
      *>      written MOVE -12 TO G stores the same         => [-12 ]
      *>  A1  N3 PIC 9(3) = 123 into PIC N ANY LENGTH: rule 2c makes the
      *>      length match, rule 2d's Table 16 says integer -> national
      *>      Yes                                           => [123]
      *> Each line is printed by the CALL lane (C:) and then the INVOKE
      *> lane (I:).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1113K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1113K.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:E=[" L "]"
           GOBACK.
       END METHOD ME.
       IDENTIFICATION DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC N(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "I:N=[" L "]"
           GOBACK.
       END METHOD MN.
       IDENTIFICATION DIVISION.
       METHOD-ID. MX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "I:X=[" L "]"
           GOBACK.
       END METHOD MX.
       IDENTIFICATION DIVISION.
       METHOD-ID. MG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 L1 PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "I:G=[" L "]"
           GOBACK.
       END METHOD MG.
       IDENTIFICATION DIVISION.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC N ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "I:A=[" L "]"
           GOBACK.
       END METHOD MA.
       END OBJECT.
       END CLASS PB1113K.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1113K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1113K.
       01 N3 PIC 9(3) VALUE 123.
       01 B1 PIC 1(4) VALUE B"1100".
       01 B2 PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1113K "NEW" RETURNING O
           CALL "PB1113E" AS NESTED USING BY CONTENT 123
           INVOKE O "ME" USING BY CONTENT 123
           CALL "PB1113E" AS NESTED USING BY CONTENT 12.5
           INVOKE O "ME" USING BY CONTENT 12.5
           CALL "PB1113N" AS NESTED USING BY CONTENT 12
           INVOKE O "MN" USING BY CONTENT 12
           CALL "PB1113N" AS NESTED USING BY CONTENT B"1"
           INVOKE O "MN" USING BY CONTENT B"1"
           CALL "PB1113N" AS NESTED USING BY CONTENT B1 B-AND B2
           INVOKE O "MN" USING BY CONTENT B1 B-AND B2
           CALL "PB1113X" AS NESTED USING BY CONTENT -5
           INVOKE O "MX" USING BY CONTENT -5
           CALL "PB1113G" AS NESTED USING BY CONTENT B"10"
           INVOKE O "MG" USING BY CONTENT B"10"
           CALL "PB1113G" AS NESTED USING BY CONTENT -12
           INVOKE O "MG" USING BY CONTENT -12
           CALL "PB1113A" AS NESTED USING BY CONTENT N3
           INVOKE O "MA" USING BY CONTENT N3
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113E.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:E=[" L "]"
           GOBACK.
       END PROGRAM PB1113E.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC N(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:N=[" L "]"
           GOBACK.
       END PROGRAM PB1113N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:X=[" L "]"
           GOBACK.
       END PROGRAM PB1113X.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113G.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 L1 PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "C:G=[" L "]"
           GOBACK.
       END PROGRAM PB1113G.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC N ANY LENGTH.
       PROCEDURE DIVISION USING L.
           DISPLAY "C:A=[" L "]"
           GOBACK.
       END PROGRAM PB1113A.
       END PROGRAM PB1113R.
