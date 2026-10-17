*>--- conformance-case
*> id: pb88-unstring-pointer-not-integer
*> rules: [SR-14.9.48.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.48.3 SR6: identifier-7 (the POINTER) "shall be described as an elementary numeric integer data
      *> item ... The symbol 'P' shall not be used" - a PIC 9V9 pointer is not one. kb/Work PB88: COBOLNET1651 at
      *> bind (a run-time stage before).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NUNSPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(6) VALUE "ABCDEF".
       01 R PIC X(3).
       01 P PIC 9V9 VALUE 1.
       PROCEDURE DIVISION.
           UNSTRING S DELIMITED BY "C" INTO R WITH POINTER P.
           STOP RUN.
