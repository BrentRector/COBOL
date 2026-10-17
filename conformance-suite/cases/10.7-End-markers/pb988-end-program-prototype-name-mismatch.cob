      *>--- conformance-case
      *> id: pb988-end-program-prototype-name-mismatch
      *> rules: [SR-10.7.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR8:
      *>   "Program-prototype-name-1 shall be identical to the
      *>   program-prototype-name declared in the corresponding
      *>   PROGRAM-ID paragraph."
      *>   cite.py: OK  10.7.3 8)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB988P IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM PB988Q.
