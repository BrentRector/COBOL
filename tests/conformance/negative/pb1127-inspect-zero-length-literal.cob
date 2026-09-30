      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1127 - ISO 14.9.22.3 SR3 (cite.py --check 14.9.22.3 "shall not be a zero-length literal" -> OK 3):
      *> "Literal-1, literal-2, literal-3, literal-4, or literal-5 shall not be a zero-length literal." A
      *> zero-length TALLYING literal-1 compared against nothing and counted zero; the same screen covers
      *> every literal position. Expected: COBOLNET1757 (statement-operand-rule) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1127ZL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "AAXAA".
       01 N PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT X TALLYING N FOR ALL ""
           DISPLAY N
           STOP RUN.
