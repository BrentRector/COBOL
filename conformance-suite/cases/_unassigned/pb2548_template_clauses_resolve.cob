*>--- conformance-case
*> id: pb2548_template_clauses_resolve
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2548 - the positive pin of the TYPEDEF template screens. A type declaration has no storage (ISO 13.18.58.4
      *> GR2, cite.py OK), but its clauses are screened as written (ISO 4.3): UNUSED-LIB below carries a RENAMES THRU
      *> (ISO 13.18.45.3 SR4) and an OCCURS ... ASCENDING KEY (ISO 13.18.38.3 SR3) whose operands are all members of the
      *> declaration, so the screens accept it. LIB's OCCURS DEPENDING ON counter LIB-CNT is NOT a member of the
      *> declaration: ISO 13.18.57.4 GR1 (cite.py OK) makes a TYPE clause "as though the data description identified by
      *> type-name-1 had been coded in place", and ISO 8.4.2.2.1 rule 5 resolves a data-name in a data description entry
      *> clause among the groups superordinate to the subject, so the counter is the sibling LIB-CNT of the use site USE-IT.
      *> A template whose counter the use site supplies is legal source and must stay accepted. USE-IT holds the table at
      *> its current count (ISO 13.18.38.4 GR8): LIB-CNT is 2, so the group holds two occurrences of LIB-TBL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2548OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LIB IS TYPEDEF.
          05 LIB-TBL PIC X OCCURS 1 TO 3 DEPENDING ON LIB-CNT.
       01 UNUSED-LIB IS TYPEDEF.
          05 UL-FIRST PIC X(2).
          05 UL-SECOND PIC X(2).
          05 UL-ROWS OCCURS 2 ASCENDING KEY IS UL-ROW-KEY.
             10 UL-ROW-KEY PIC 9.
          66 UL-BOTH RENAMES UL-FIRST THRU UL-SECOND.
       01 REC.
          05 LIB-CNT PIC 9 VALUE 2.
          05 USE-IT TYPE LIB.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "XYZ" TO USE-IT
           DISPLAY LIB-CNT " " USE-IT
           STOP RUN.
