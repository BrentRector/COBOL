      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR1:
      *>   "An end marker shall be present in every source unit that
      *>   contains, is contained in, or precedes another source unit."
      *>   cite.py: OK  10.7.3 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988D.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "FIRST".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988DS.
       PROCEDURE DIVISION.
       SMAIN.
           DISPLAY "SECOND".
           EXIT PROGRAM.
