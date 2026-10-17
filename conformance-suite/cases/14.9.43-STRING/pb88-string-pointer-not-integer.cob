*>--- conformance-case
*> id: pb88-string-pointer-not-integer
*> rules: [SR-14.9.43.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR7: identifier-4 (the POINTER) "shall be described as an elementary numeric integer data
      *> item ... The symbol 'P' shall not be used" - a PIC 9V9 pointer is not one. kb/Work PB88: COBOLNET1651.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NSTRPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(10).
       01 P PIC 9V9 VALUE 1.
       PROCEDURE DIVISION.
           STRING "AB" DELIMITED SIZE INTO R WITH POINTER P.
           STOP RUN.
