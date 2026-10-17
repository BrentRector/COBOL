      *>--- conformance-case
      *> id: pb988-end-program-missing-container
      *> rules: [SR-10.7.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR1:
      *>   "An end marker shall be present in every source unit that
      *>   contains, is contained in, or precedes another source unit."
      *>   cite.py: OK  10.7.3 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988C.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB988CN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988CN.
       PROCEDURE DIVISION.
       NMAIN.
           DISPLAY "NESTED".
           EXIT PROGRAM.
       END PROGRAM PB988CN.
