*>--- conformance-case
*> id: pb695-erase-no-end-of
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OMITTED OPTIONAL WORDS MUST REACH THE DOCUMENTED REFUSAL, NOT A PARSE ERROR (PB695).
      *> ISO 13.18.21.2 prints `ERASE { END OF LINE | END OF SCREEN | EOL | EOS }`. Measured on
      *> printed page 429 / folio 399: ERASE carries an underline rectangle (92.2% cover), LINE one at
      *> 89.7%, SCREEN 94.0%, EOL 87.9% and EOS 87.5% - while BOTH occurrences of END (boxes
      *> 127.54-148.05 and 127.53-148.03) and BOTH of OF (150.39-163.11 and 150.37-163.09) have NO
      *> rule in their bands. 8.3.2.4.3 therefore makes `ERASE LINE` a conforming spelling of the
      *> clause, and 13.18.21.3 SR1/SR2 confirm it is the same clause EOL and EOS abbreviate.
      *> WHY THIS IS A NEGATIVE CASE. The ERASE clause belongs to the DECLINED screen module (Annex
      *> A.4.2 items 8 and 22), and 4.2.7 makes non-support conformant only when it is DIAGNOSED -
      *> COBOLNET1560 is that diagnosis, and it is what proves the clause was RECOGNIZED rather than
      *> mis-parsed. The brace is still a REQUIRED choice (5.2.6.3), so a bare `ERASE` remains a
      *> syntax error; this program writes the shortest LEGAL spelling instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695ERASENOEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       SCREEN SECTION.
       01 S1.
          05 LINE 1 COLUMN 1 ERASE LINE.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE"
           STOP RUN.
