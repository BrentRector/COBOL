*>--- conformance-case
*> id: dyn_implicit_grow
*> rules: [GR-8.5.1.9.1-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> OCCURS DYNAMIC implicit growth (increment 3, data-model D9; ISO 8.5.1.9.2/.9.3). A RECEIVING subscript past
      *> the current capacity GROWS the table to it, seeding the skipped intermediate occurrences (INITIALIZED); the
      *> CAPACITY register tracks the growth. A SENDING subscript out of range continues benignly (checking off, the
      *> COBOL-85 default) with a fresh default element -- never a growth, never a crash. FROM 2: MOVE 42 TO ELT(5)
      *> grows to 5 (occ 3-4 seeded 0); ELT(9) read (sending, > capacity) is benign 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-IMPLICIT-GROW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED PIC ZZ9.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 42 TO WS-E (5).
           MOVE WS-CAP TO ED.
           DISPLAY "CAP=[" ED "]".
           DISPLAY "E5=" WS-E (5).
           DISPLAY "E3=" WS-E (3).
           DISPLAY "E9=" WS-E (9).
           STOP RUN.
