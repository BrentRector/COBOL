      *> reject-at: 2002 2014 2023
      *> kb/Work PB1946 (verdict kb/Work PB1936) -- ISO 14.8.2.3.3 2) d) asks
      *> the MOVE question of the argument's VALUE, and an arithmetic
      *> expression is a numeric sender whose value carries no compile-time
      *> integer guarantee, so Table 16's NONINTEGER numeric row applies:
      *> 14.9.25.3 Table 16, Numeric Noninteger -> "Alphanumeric-edited,
      *> Alphanumeric" is "No". The BY CONTENT expression N3 * 2 into the
      *> PIC X(4) nested-program formal is COBOLNET1688. (The same pair into
      *> a numeric-edited formal is "Yes": pb1946_content_expression_move_regime.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N3 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1946XS" AS NESTED USING BY CONTENT N3 * 2
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946XS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "L=[" L "]"
           GOBACK.
       END PROGRAM PB1946XS.
       END PROGRAM PB1946X.
