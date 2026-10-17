*>--- conformance-case
*> id: based_pointer
*> rules: [GR-13.18.5.4-1, GR-14.9.39.4-13, FMT-13.18.5.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.5 / §14.9.39 / §8.4.3 — BASED data + ADDRESS OF + SET ADDRESS OF (COBOL-2002).
      *> A BASED item has NO storage of its own; it is addressed at use time through its data-address
      *> pointer (a managed ManagedPointer, never an 8-byte handle). Slice 1b boundary 2:
      *>   SET p TO ADDRESS OF x   — take a managed pointer to x's storage (x becomes byte-backed).
      *>   SET ADDRESS OF b TO p   — rebase the BASED item b onto that storage.
      *>   a reference to b then reads (DISPLAY) and writes (MOVE) x's bytes through the pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BASEDPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC X(5) VALUE "HELLO".
       01 P  USAGE POINTER.
       01 B  PIC X(5) BASED.
       PROCEDURE DIVISION.
       MAIN.
      *> Point P at X, then make the BASED item B address X's storage.
           SET P TO ADDRESS OF X.
           SET ADDRESS OF B TO P.
      *> Reading B reads X's bytes through the pointer.
           DISPLAY "B=" B.
      *> Writing through B updates X's storage (same bytes).
           MOVE "WORLD" TO B.
           DISPLAY "X=" X.
           STOP RUN.
