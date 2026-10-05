      *> ALL ZERO WHERE THE LITERAL IS NUMERIC-RESTRICTED, AT COBOL-85 (kb/Work PB1415).
      *> ISO/IEC 1989:2023 §8.3.3.6.3 SR1 a) admits ZERO in a numeric-restricted
      *> position only "without the ALL phrase"; the edge is DERIVED at 2002 (VCR Table 7
      *> row 7.28), and at 85 the word ALL before a figurative word is redundant: CCVS-85
      *> writes VALUE ALL ZEROS on PIC 999 (NC201A). So each ALL ZERO below IS the
      *> figurative ZERO (§8.3.3.6.4 GR4, the numeric value 0):
      *>   V6 PIC 9(3) VALUE ALL ZEROES      -> 000
      *>   V7 packed VALUE ALL ZEROS          -> V7 ZERO
      *>   88 C4 VALUE ALL ZERO over V4 = 0   -> C4 TRUE
      *>   ADD ALL ZERO TO N (N = 5)          -> 005
      *>   IF ALL ZEROS IS POSITIVE (0 > 0)   -> NOT POS
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1415AZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V6 PIC 9(3) VALUE ALL ZEROES.
       01 V7 PIC S9(3)V9 PACKED-DECIMAL VALUE ALL ZEROS.
       01 V4 PIC 9 VALUE 0.
          88 C4 VALUE ALL ZERO.
       01 N PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY V6
           IF V7 = ZERO
               DISPLAY "V7 ZERO"
           ELSE
               DISPLAY "V7 NONZERO"
           END-IF
           IF C4
               DISPLAY "C4 TRUE"
           ELSE
               DISPLAY "C4 FALSE"
           END-IF
           ADD ALL ZERO TO N
           DISPLAY N
           IF ALL ZEROS IS POSITIVE
               DISPLAY "POS"
           ELSE
               DISPLAY "NOT POS"
           END-IF
           STOP RUN.
