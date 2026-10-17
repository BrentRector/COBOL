*>--- conformance-case
*> id: pb1030-all-subscript-outside-function
*> rules: [SR-8.4.2.3.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1030 - ISO 8.4.2.3.3 SR6: "The subscript ALL may be used only" in an
      *> intrinsic function argument or a SORT table's rightmost subscript. A MOVE receiver is
      *> neither.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1030B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X OCCURS 5.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           MOVE "Z" TO E (ALL).
           STOP RUN.
