      *> kb/Work PB1643, the 2002 forms of the literal image. ISO
      *> 14.9.11.4 GR1 leaves the conversion of a literal to the device
      *> to the implementor (CONFORMANCE.md DOC-A.1-56: the literal AS
      *> WRITTEN); 12.3.7.4 GR14 a) makes the comma the character
      *> "written in numeric literals to represent the decimal
      *> separator". 13.10.4 GR1: a constant-name stands "as if literal-1
      *> ... were written where constant-name-1 is written", so DISPLAY
      *> KC prints what DISPLAY 1,5 prints. 8.3.3.3.3: the significand of
      *> a floating-point literal is a fixed-point literal, so it is
      *> written with the comma too. An INVOKE BY CONTENT literal-2 is the
      *> same literal: 12,5 reaches the PIC ZZ9,99 formal (14.8.2.3.3 2a:
      *> the COMPUTE rule) as 12.5, edited " 12,50" - before the fix its
      *> raw text '12,5' was never normalized and the backend refused
      *> the generated code.
      *> EXPECTED: 1,5 / 1,5 / -2,25 / 1,5E3 / I:  12,50 / I:  12,00 / 3,5 (the formal
      *> is edited ZZ9,99, so the method prints one leading space).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1643K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       REPOSITORY. CLASS BASE CLASS PB1643K.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9,99.
       PROCEDURE DIVISION USING L.
           DISPLAY "I: " L
           GOBACK.
       END METHOD ME.
       END OBJECT.
       END CLASS PB1643K.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1643R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       REPOSITORY. CLASS PB1643K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KC CONSTANT AS 1,5.
       01 KN CONSTANT AS -2,25.
       01 O USAGE OBJECT REFERENCE PB1643K.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY 1,5
           DISPLAY KC
           DISPLAY KN
           DISPLAY 1,5E3
           INVOKE PB1643K "NEW" RETURNING O
           INVOKE O "ME" USING BY CONTENT 12,5
           INVOKE O "ME" USING BY CONTENT 12
           DISPLAY 3,5 WITH NO ADVANCING
           DISPLAY SPACE
           STOP RUN.
       END PROGRAM PB1643R.
