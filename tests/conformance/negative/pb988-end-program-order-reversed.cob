      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR3:
      *>   "If a PROGRAM-ID paragraph declaring a specific program-name
      *>   is stated between the PROGRAM-ID paragraph and the END
      *>   PROGRAM marker for program-name-1, then an END PROGRAM
      *>   marker referencing program-name shall precede the END
      *>   PROGRAM marker referencing program-name-1."
      *>   cite.py: OK  10.7.3 3)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988B.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB988BN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988BN.
       PROCEDURE DIVISION.
       NMAIN.
           DISPLAY "NESTED".
           EXIT PROGRAM.
       END PROGRAM PB988B.
       END PROGRAM PB988BN.
