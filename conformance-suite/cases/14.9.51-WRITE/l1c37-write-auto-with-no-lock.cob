*>--- conformance-case
*> id: l1c37-write-auto-with-no-lock
*> rules: [SR-14.9.51.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.51.3 SR22 — WRITE WITH NO LOCK under LOCK MODE AUTOMATIC
      *> Rule: "If automatic locking has been specified for the write
      *>   file, neither the WITH LOCK phrase nor the WITH NO LOCK
      *>   phrase shall be specified."
      *>   cite.py --check 14.9.51.3 "If automatic locking has been
      *>   specified for the write file, neither the WITH LOCK phrase
      *>   nor the WITH NO LOCK phrase shall be specified"
      *>   -> OK  §14.9.51.3 22)  (Syntax rules)
      *> The file specifies LOCK MODE IS AUTOMATIC and the WRITE carries
      *> WITH NO LOCK; everything else is valid (format 2 on a relative file,
      *> SR3), so the only reason to reject is SR22. The lock phrases
      *> are COBOL-2002 introductions, hence no 85 leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C37H.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "L1C37H.REL"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               LOCK MODE IS AUTOMATIC.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC.
           05 RDATA PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
       M1.
           OPEN I-O RELF
           MOVE 1 TO RK
           MOVE "R001" TO RDATA
           WRITE RREC WITH NO LOCK
               INVALID KEY DISPLAY "INVALID"
           END-WRITE
           CLOSE RELF
           STOP RUN.
