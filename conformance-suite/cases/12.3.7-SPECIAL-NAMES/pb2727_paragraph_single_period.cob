*>--- conformance-case
*> id: pb2727_paragraph_single_period
*> rules: [FMT-12.3.7.2, FMT-12.3.8.2, FMT-12.4.6.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - the I-O-CONTROL, SPECIAL-NAMES and REPOSITORY paragraphs each print ONE
      *> separator period after their entries (ISO 12.4.6.2, 12.3.7.2, 12.3.8.2; 5.2.1 binds the
      *> punctuation a general format prints).  Each paragraph below writes several entries and that one
      *> closing period, and the program compiles and runs: the CLASS and SYMBOLIC CHARACTERS clauses
      *> of one SPECIAL-NAMES sentence both take effect, as do both intrinsic specifiers of one
      *> REPOSITORY sentence (each name usable without the word FUNCTION, 12.3.8.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2727MN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS DIGITS-ONLY IS "0" THRU "9"
           SYMBOLIC CHARACTERS LETTER-A IS 66.
       REPOSITORY.
           FUNCTION UPPER-CASE INTRINSIC
           FUNCTION REVERSE INTRINSIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "P2727F1.dat".
           SELECT F2 ASSIGN TO "P2727F2.dat".
           SELECT F3 ASSIGN TO "P2727F3.dat".
           SELECT F4 ASSIGN TO "P2727F4.dat".
       I-O-CONTROL.
           SAME AREA FOR F1 F2
           SAME RECORD AREA FOR F3 F4.
       DATA DIVISION.
       FILE SECTION.
       FD F1. 01 R1 PIC X(4).
       FD F2. 01 R2 PIC X(4).
       FD F3. 01 R3 PIC X(4).
       FD F4. 01 R4 PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-NUM  PIC X(3) VALUE "123".
       01 WS-TEXT PIC X(3).
       PROCEDURE DIVISION.
           IF WS-NUM IS DIGITS-ONLY
               DISPLAY "CLASS OK"
           END-IF
           DISPLAY "SYMBOLIC " LETTER-A
           MOVE UPPER-CASE("abc") TO WS-TEXT
           DISPLAY WS-TEXT
           MOVE REVERSE("abc") TO WS-TEXT
           DISPLAY WS-TEXT
           STOP RUN.
