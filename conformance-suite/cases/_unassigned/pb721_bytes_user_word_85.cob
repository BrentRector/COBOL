      *>--- conformance-case
      *> id: pb721_bytes_user_word_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE OVER-REJECTION TWIN of the BYTES edition gate (kb/Work PB721).
      *> BYTES is a 8.10 CONTEXT-SENSITIVE word, not an 8.9 reserved word, at every
      *> supported edition, and 8.10 says what that means: "If a context-sensitive word
      *> is used where the context-sensitive word is permitted in the general format,
      *> the word is treated as a keyword; otherwise it is treated as a user-defined
      *> word." So gating the RECORD clause's BYTES SPELLING at 2023 may not cost a
      *> COBOL-85 program the right to NAME something BYTES - and adding a hard lexer
      *> token is exactly how that right gets lost silently.
      *> This program names an elementary item BYTES, a TABLE BYTES (so the subscript
      *> trigger is exercised too - a keyword token that never enters SUBSCRIPT mode
      *> turns `BYTES (1)` into a parse error rather than a reference), and a paragraph
      *> BYTES-PARA, and compiles at --std 85.
      *> EXPECTED VALUES: the moves and the subscripted references are ordinary
      *> alphanumeric ones - BYTES holds "USERWD", and the three table positions read
      *> back P, Q and R in order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB721-BYTES-USER-85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BYTES     PIC X(6) VALUE "USERWD".
       01  WS-TAB.
           05  WS-B  OCCURS 3 TIMES PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "PQR" TO WS-TAB
           PERFORM BYTES-PARA
           DISPLAY "W=" BYTES
           DISPLAY "T=" WS-B(1) WS-B(2) WS-B(3)
           STOP RUN.
       BYTES-PARA.
           DISPLAY "PARA".
