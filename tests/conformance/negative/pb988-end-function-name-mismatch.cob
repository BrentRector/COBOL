      *> reject-at: 2002 2014 2023
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR7:
      *>   "User-function-name-1 shall be identical to the
      *>   user-function-name declared in the corresponding FUNCTION-ID
      *>   paragraph."
      *>   cite.py: OK  10.7.3 7)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB988F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9.
       PROCEDURE DIVISION RETURNING LK-R.
       FMAIN.
           MOVE 7 TO LK-R.
           GOBACK.
       END FUNCTION PB988G.
