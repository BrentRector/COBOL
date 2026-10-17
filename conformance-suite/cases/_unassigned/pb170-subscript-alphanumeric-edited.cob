*>--- conformance-case
*> id: pb170-subscript-alphanumeric-edited
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The PB155 edited shape, at the resolver arm PB155's widening could not
      *> reach. An alphanumeric-EDITED picture is modelled as category
      *> Alphanumeric with an EditMask - edited or plain, 8.5.2.1 Table 2 makes
      *> it class alphanumeric, which is not class numeric (8.8.1.1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC XXBXX VALUE "AB CD".
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           MOVE E(XE) TO R
           STOP RUN.
