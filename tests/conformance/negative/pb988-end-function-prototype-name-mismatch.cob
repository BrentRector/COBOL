      *> reject-at: 2002 2014 2023
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR9:
      *>   "Function-prototype-name-1 shall be identical to the
      *>   function-prototype-name declared in the corresponding
      *>   FUNCTION-ID paragraph."
      *>   cite.py: OK  10.7.3 9)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB988FP IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9.
       PROCEDURE DIVISION RETURNING LK-R.
       END FUNCTION PB988FQ.
