*>--- conformance-case
*> id: pb124-vlg-function-argument
*> rules: [AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 (the trailing block): "A variable-length group shall be referenced as an argument to a
      *> function only when explicitly permitted in the function definition." A variable-length group per the
      *> 8.5.1.12.1 DEFINITION is one with a dynamic-length elementary item or dynamic-capacity table
      *> subordinate (an OCCURS DEPENDING group is a FIXED-length group and stays a legal argument - the
      *> first cut of this screen used the broader runtime-length predicate and the gate's pb61 leg caught
      *> it). Only LENGTH (15.50.4 r7) and BYTE-LENGTH (15.14.4 r6) define a variable-length-group value;
      *> UPPER-CASE does not (kb/Work PB124 wave 4, AR-15.3-14).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VLG.
          05 FIXED-PART PIC X(2).
          05 VD PIC X DYNAMIC LENGTH.
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION UPPER-CASE(VLG) TO RS
           STOP RUN.
