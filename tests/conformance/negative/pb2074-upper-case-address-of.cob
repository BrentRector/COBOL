      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2074 - ISO/IEC 1989:2023 15.97.3 r1: argument-1 of
      *> UPPER-CASE shall be of class alphabetic, alphanumeric, or
      *> national. ADDRESS OF X creates a unique data item of class
      *> pointer (8.4.3.11.4 GR1), which holds an address and no
      *> characters, so it is refused (COBOLNET1627) at every edition and
      *> under --permissive too (IntrinsicPointerArgumentPermissiveTests):
      *> the migration mode's coercion decodes characters and a pointer
      *> has none. Had it compiled, IntrinsicRenderer's address-identifier
      *> arm would have aborted the program at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2074UCA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "abcd".
       01 R PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION UPPER-CASE (ADDRESS OF X) TO R
           DISPLAY "R=" R
           STOP RUN.
