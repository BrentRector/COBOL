*>--- conformance-case
*> id: pb169-status-index-name
*> rules: [SR-14.9.42.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.38.3 syntax rule 7 closes the contexts that may reference an
      *> index-name to five - a subscript, PERFORM VARYING, SEARCH VARYING, SET,
      *> and an operand in a relation condition. The termination-status phrase is
      *> not among them, and 8.4.3.1.2 makes an index-name not an identifier, so
      *> 14.9.42.3 SR2's identifier-1 cannot be one either. The R16 screen for
      *> exactly this rule already existed and was simply not applied here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB169N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC X OCCURS 3 TIMES INDEXED BY WS-IX.
       PROCEDURE DIVISION.
       MAIN.
           SET WS-IX TO 2
           STOP RUN WITH ERROR STATUS WS-IX.
