      *> reject-at: 2002 2014 2023
      *> kb/Work PB1430 - ISO 8.4.3.4.2 general format (rendered from the
      *> canonical PDF page 163, printed folio 133):
      *>   {object-class-name-1 | identifier-1} :: literal-1
      *>     [ ( {arithmetic-expression-1 | boolean-expression-1 |
      *>          identifier-2 | literal-2 | OMITTED} ... ) ]
      *> The BRACKET makes the whole parenthesised group optional, but the
      *> argument forms sit in BRACES whose ellipsis repeats them INSIDE
      *> the one pair, and 5.2.6.3 says one alternative of a brace group
      *> "shall be explicitly specified". So a written pair holds at least
      *> one argument, and A1 :: "GETNAME" ( ) is no spelling the format
      *> prints. It used to compile and run (W=ACCOUNT). COBOLNET2994.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1430N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1430N1C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB1430N1C.
       01 W  PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1430N1C "NEW" RETURNING A1.
           MOVE A1 :: "GETNAME" ( ) TO W.
           DISPLAY W.
           STOP RUN.
       END PROGRAM PB1430N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1430N1C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(7).
       PROCEDURE DIVISION RETURNING R.
       MAIN.
           MOVE "ACCOUNT" TO R.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB1430N1C.
