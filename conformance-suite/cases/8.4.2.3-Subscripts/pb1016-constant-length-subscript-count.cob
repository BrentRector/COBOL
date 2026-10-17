*>--- conformance-case
*> id: pb1016-constant-length-subscript-count
*> rules: [SR-8.4.2.3.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1016 - ISO 8.4.2.3.3 SR3 over a constant entry's LENGTH OF operand (13.10, 2002+).
      *> SR3: "the number of subscripts shall equal the number of OCCURS clauses in the description of the table
      *> element being referenced". CELL is subordinate to TWO OCCURS clauses and ONE subscript is written - hung
      *> off the qualification, the spelling the constant binder's private suffix walk never looked at.
      *> Rejected by the ONE procedure-division screen, COBOLNET2097. (85 has no constant entry at all.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1016NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 ROWX OCCURS 3.
             10 CELL PIC X(4) OCCURS 2.
       01 KC CONSTANT AS LENGTH OF CELL OF ROWX (1).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
