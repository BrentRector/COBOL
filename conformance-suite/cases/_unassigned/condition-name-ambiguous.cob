*>--- conformance-case
*> id: condition-name-ambiguous
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R33's sweep - the condition-name sibling (8.4.2.2 Format 2): two level-88s named
      *> IS-ON under different conditional variables, referenced unqualified. The reference must
      *> identify exactly one 88; previously the first declaration won silently. Qualify by the
      *> conditional variable (IS-ON OF A1) to disambiguate; --permissive keeps first-match, warned.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R33NEGB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC 9 VALUE 1.
          88 IS-ON VALUE 1.
       01 A2 PIC 9 VALUE 0.
          88 IS-ON VALUE 1.
       PROCEDURE DIVISION.
           IF IS-ON DISPLAY "ON" END-IF.
           STOP RUN.
