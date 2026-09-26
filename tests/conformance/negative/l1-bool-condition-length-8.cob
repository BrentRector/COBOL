      *> reject-at: 2002 2014 2023
      *> ISO §8.8.4.3.3 SR1 — "Boolean-expression-1 shall reference only
      *> boolean items of length 1." B8 is a boolean item of length 8
      *> (PICTURE 1(8)), so the simple boolean condition IF B8 violates
      *> the rule and shall be refused at compile time. Nothing else in
      *> the program is nonconforming: the VALUE is an 8-position boolean
      *> literal for an 8-position boolean item, and the IF statement is
      *> otherwise well formed. Boolean items arrive at COBOL 2002, so
      *> the rule has no subject at COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1BCL8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8 PIC 1(8) VALUE B"00000001".
       PROCEDURE DIVISION.
       MAIN-P.
           IF B8
               DISPLAY "TRUE"
           END-IF
           STOP RUN.
