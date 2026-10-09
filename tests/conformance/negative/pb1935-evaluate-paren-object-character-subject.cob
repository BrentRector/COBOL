      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1935 -- the EVALUATE twin of the relation case: the
      *> object (C) of an alphanumeric subject A, C PIC 9, was read as
      *> the bare item C. ISO 14.9.13.4 4) a) 6.: the pair "is
      *> considered to be a conditional expression" A = (C), whose
      *> (C) is an arithmetic expression (8.8.1.1), which 8.8.4.2.5
      *> does not admit against a character operand (cite.py: OK for
      *> each). COBOLNET2532 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1935E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X VALUE "3".
       01 C PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE A
             WHEN (C) DISPLAY "T"
             WHEN OTHER DISPLAY "F"
           END-EVALUATE.
           STOP RUN.
