      *> ISO 11.9.2 (kb/Work PB1508): all seven OPTIONS clauses, written in the sequence the general
      *> format prints them (5.2.1 - no 11.9.3 rule frees the order), compile and take effect.
      *> INITIALIZE writes its storage-section alternatives WORKING-STORAGE then LOCAL-STORAGE: they sit in
      *> CHOICE INDICATORS (11.9.10.2), so 5.2.6.4 admits them "in any order", each once.
      *> Expected values from the spec:
      *>   - DEFAULT ROUNDED MODE IS TRUNCATION (11.9.6): COMPUTE B ROUNDED = 1.5 into PIC 9 gives 1.
      *>   - INITIALIZE ... TO X"5A" (11.9.10.4): W, with no VALUE clause, starts as "ZZZZ".
      *> FLOAT-DECIMAL is the declined A.3 item 13 clause (COBOLNET2424, a warning) and is accepted inert.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508S.
       OPTIONS.
           ARITHMETIC IS NATIVE
           DEFAULT ROUNDED MODE IS TRUNCATION
           ENTRY-CONVENTION IS COBOL
           FLOAT-BINARY IS HIGH-ORDER-LEFT
           FLOAT-DECIMAL IS HIGH-ORDER-RIGHT DECIMAL-ENCODING
           INITIALIZE WORKING-STORAGE LOCAL-STORAGE TO X"5A"
           INTERMEDIATE ROUNDING IS NEAREST-EVEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9V9 VALUE 1.5.
       01 B PIC 9.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE B ROUNDED = A.
           DISPLAY "B=" B " W=[" W "]".
           STOP RUN.
