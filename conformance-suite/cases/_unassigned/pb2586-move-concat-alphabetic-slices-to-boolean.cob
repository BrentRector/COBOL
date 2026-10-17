*>--- conformance-case
*> id: pb2586-move-concat-alphabetic-slices-to-boolean
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.18.4 r3 + 14.9.25.3 Table 16 (kb/Work PB2586, review R1-1): CONCAT of two reference-modified
      *> slices of PIC A items is alphabetic (8.4.3.3.4 GR6 keeps the category, kb/Work PB73), and the
      *> Alphabetic row makes a boolean receiver "No" - exactly as for MOVE A(1:2) TO the receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2586N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC A(5) VALUE "ABCDE".
       01 WS-B PIC 1(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT(WS-A(1:2) WS-A(3:2)) TO WS-B
           STOP RUN.
