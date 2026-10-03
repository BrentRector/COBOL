      *> reject-at: 2002 2014 2023
      *> kb/Work PB1946 (verdict kb/Work PB1936) -- ISO 14.8.2.3.3 2) d) asks
      *> the MOVE question of the argument's VALUE, and an arithmetic
      *> expression is a numeric sender whose value carries no compile-time
      *> integer guarantee: 14.9.25.3 Table 16, Numeric Noninteger ->
      *> "National, National-edited" is "No". The BY CONTENT expression
      *> N3 * 2 into the PIC N(4) method formal is COBOLNET0828.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1946NK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1946NK.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC N(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "IN-MN"
           GOBACK.
       END METHOD MN.
       END OBJECT.
       END CLASS PB1946NK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946NM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1946NK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1946NK.
       01 N3 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1946NK "NEW" RETURNING O
           INVOKE O "MN" USING BY CONTENT N3 * 2
           STOP RUN.
       END PROGRAM PB1946NM.
