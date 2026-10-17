*>--- conformance-case
*> id: pb72-move-alphabetic-to-numeric
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.25.3 SR10, Table 16: the ALPHABETIC row's Numeric and
      *> Numeric-edited columns are "No" (the ALPHANUMERIC row's are "Yes" - the
      *> classic X-to-9 move - which is exactly why the finer alphabetic axis
      *> must be carried). Before PB72 a PIC A sender slid through as
      *> alphanumeric and MOVE ABCD TO a PIC 9 item silently stored zeros.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB72NEGAN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC A(4) VALUE "ABCD".
       01 WS-9 PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
           MOVE WS-A TO WS-9
           STOP RUN.
