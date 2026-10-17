      *>--- conformance-case
      *> id: pb988-end-program-name-mismatch
      *> rules: [SR-10.7.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR2:
      *>   "Program-name-1 shall be identical to the program-name
      *>   declared in a preceding PROGRAM-ID paragraph."
      *>   cite.py: OK  10.7.3 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988A.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "RAN".
           STOP RUN.
       END PROGRAM PB988Z.
