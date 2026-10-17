*>--- conformance-case
*> id: pb189-dynamic-table-element-operand-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB189 - the edition floor under conformance:cases/14.9.11-DISPLAY/pb189_dynamic_table_element_operand.
      *> A dynamic-capacity table (8.5.1.9; OCCURS Format 4, 13.18.38) is a COBOL-2014 introduction, so at
      *> COBOL-85 and COBOL-2002 the table whose ELEMENT the positive case displays cannot be declared at
      *> all; COBOLNET0900 is the version gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB189N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GP PIC X(3) VALUE "PFX".
          05 GT OCCURS DYNAMIC CAPACITY IN CAP FROM 1.
             10 GT-N PIC 9(2).
             10 GT-A PIC X.
       PROCEDURE DIVISION.
           MOVE 42 TO GT-N(1)
           DISPLAY GT(1)
           STOP RUN.
