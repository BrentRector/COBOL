*>--- conformance-case
*> id: pb124-vlg-type-argument
*> rules: [AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3: "If any function permits a type declaration as an argument, the type declaration shall
      *> not describe a variable-length group." TVLG's DYNAMIC LENGTH subordinate makes it one per the
      *> 8.5.1.12.1 definition (an OCCURS DEPENDING typedef is FIXED-length and stays legal - 15.50.4 r4a
      *> defines LENGTH over it, pinned by pb61). LENGTH(TVLG) answered a width silently before (kb/Work
      *> PB124 wave 4, AR-15.3-14).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TVLG TYPEDEF.
          05 TA PIC X(3).
          05 TD PIC X DYNAMIC LENGTH.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LENGTH(TVLG)
           STOP RUN.
