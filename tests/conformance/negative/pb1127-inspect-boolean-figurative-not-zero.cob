      *> reject-at: 2002 2014 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR3 (cite.py --check 14.9.22.3 "only the figurative constant ZERO may be specified" -> OK 3):
      *> "when identifier-1 is of class boolean, the figurative constant is of class boolean and only the
      *> figurative constant ZERO may be specified". HIGH-VALUE over a boolean identifier-1 compiled clean and
      *> ran. Expected: COBOLNET1757 (statement-operand-rule) at every edition that has boolean items.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1127BF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 1(4) VALUE B"0101".
       01 N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT B TALLYING N FOR ALL HIGH-VALUE
           DISPLAY N
           STOP RUN.
