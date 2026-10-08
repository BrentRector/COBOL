      *> kb/Work PB2507. ISO 14.9.11.4 GR1: any conversion of data between
      *> a DISPLAY operand and the device is defined by the implementor,
      *> so the character image of a NUMERIC function's returned value is
      *> the implementor's (CONFORMANCE.md DOC-A.1-92): the LITERAL FORM
      *> of the value, the same image DISPLAY 1,5 gives the literal
      *> (DOC-A.1-56, kb/Work PB1643).  12.3.7.4 GR14 a): under
      *> DECIMAL-POINT IS COMMA "the character written in numeric
      *> literals to represent the decimal separator shall be the comma",
      *> and 12.3.4 GR1 hands the clause to the contained program.  The
      *> comma is also GnuCOBOL 3.2's separator for these values.  Before
      *> the fix each fixed-point line below printed a period.
      *> EXPECTED, by the rule, one line each:
      *>   NUMVAL("1,5")        1,5
      *>   MAX(1,5 2,25)        2,25      MAX(X Y)       2,25
      *>   SUM(X Y)             3,75      REM(7,5 2)     1,5
      *>   NUMVAL-C("1.234,5")  1234,5    INTEGER(7,5)   7 (no separator)
      *> then the contained program's MAX(Z 1,25) = 3,75.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2507A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9V99 VALUE 1,5.
       01 Y PIC 9V99 VALUE 2,25.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION NUMVAL("1,5")
           DISPLAY FUNCTION MAX(1,5 2,25)
           DISPLAY FUNCTION MAX(X Y)
           DISPLAY FUNCTION SUM(X Y)
           DISPLAY FUNCTION REM(7,5 2)
           DISPLAY FUNCTION NUMVAL-C("1.234,5")
           DISPLAY FUNCTION INTEGER(7,5)
           CALL "PB2507B"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2507B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z PIC 9V99 VALUE 3,75.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION MAX(Z 1,25)
           EXIT PROGRAM.
       END PROGRAM PB2507B.
       END PROGRAM PB2507A.
