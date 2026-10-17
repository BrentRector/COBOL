      *>--- conformance-case
      *> id: address_of_qualified
      *> rules: [GR-8.4.3.11.4-1, FMT-8.4.3.11.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.4.3.11 ADDRESS OF with qualified and subscripted
      *> operands (2002). GR1: the address-identifier references the
      *> address of THE data item — a qualified operand resolves through
      *> §8.4.2.2 qualification; a subscripted operand addresses the
      *> OCCURRENCE (a table lays its occurrences end-to-end in the
      *> record's storage). Each taken address is re-based onto a BASED
      *> view (§14.9.39 F7 / §13.18.5 GR2-4) and read back — the DISPLAY
      *> content proves the address landed on the right storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ADDROFQP10PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-REC.
          05 W-HEAD PIC X(4) VALUE "HEAD".
          05 W-TAB.
             10 T-ENT PIC X(3) OCCURS 3.
          05 W-TAIL PIC X(4) VALUE "TAIL".
       01 W-I    PIC 9.
       01 W-PTR  USAGE POINTER.
       01 V-3 BASED PIC X(3).
       01 V-4 BASED PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AAA" TO T-ENT(1).
           MOVE "BBB" TO T-ENT(2).
           MOVE "CCC" TO T-ENT(3).
      *> A qualified operand (§8.4.2.2).
           SET W-PTR TO ADDRESS OF W-HEAD OF W-REC.
           SET ADDRESS OF V-4 TO W-PTR.
           DISPLAY "QUAL=" V-4.
      *> A subscripted operand — the SECOND occurrence's address.
           SET W-PTR TO ADDRESS OF T-ENT(2).
           SET ADDRESS OF V-3 TO W-PTR.
           DISPLAY "SUB2=" V-3.
      *> A data-item subscript.
           MOVE 3 TO W-I.
           SET W-PTR TO ADDRESS OF T-ENT(W-I).
           SET ADDRESS OF V-3 TO W-PTR.
           DISPLAY "SUBV=" V-3.
      *> Qualified AND subscripted.
           SET W-PTR TO ADDRESS OF T-ENT OF W-TAB(1).
           SET ADDRESS OF V-3 TO W-PTR.
           DISPLAY "QSUB=" V-3.
           STOP RUN.
