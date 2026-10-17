*>--- conformance-case
*> id: pb871-dynamic-length-receiver-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB871 - the edition floor under conformance:2014/pb871_dynamic_length_receivers. A
      *> dynamic-length elementary item (8.5.1.10; the DYNAMIC LENGTH clause, 13.18.19) is a COBOL-2014
      *> introduction, so at COBOL-85 and COBOL-2002 the receiving operand these verbs store into
      *> cannot be declared at all; COBOLNET0900 is the version gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB871N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D1 PIC X DYNAMIC LENGTH LIMIT IS 8.
       PROCEDURE DIVISION.
           STRING "XY" DELIMITED BY SIZE INTO D1
           STOP RUN.
