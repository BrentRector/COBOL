*>--- conformance-case
*> id: pb3129-type-based-variable-length-group
*> rules: [SR-13.18.5.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3129 review R1-1 - ISO 13.18.5.3 SR2: "The subject of the entry shall not be a
      *> dynamic-length elementary item or a variable-length group." 13.18.57.4 GR1 codes the description
      *> of VT "in place of the TYPE clause", so G is a variable-length group (its subordinate D is a
      *> dynamic-length elementary item), exactly as the written spelling of negative
      *> pb1213-based-variable-length-group is. Before the fix this compiled and printed A=ABC D=HELLO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129BV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VT IS TYPEDEF.
          05 A PIC X(3).
          05 D PIC X DYNAMIC LENGTH.
       01 G TYPE VT BASED.
       PROCEDURE DIVISION.
           ALLOCATE G.
           MOVE "ABC" TO A OF G.
           MOVE "HELLO" TO D OF G.
           DISPLAY "A=" A OF G " D=" D OF G.
           STOP RUN.
