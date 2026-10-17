*>--- conformance-case
*> id: pb3129-type-pointer-in-weak-group
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3129 - ISO 13.18.60.3 SR14: a USAGE clause with the POINTER or PROGRAM-POINTER phrase
      *> "may be specified only for an elementary data item at level 1 or an elementary data item subordinate
      *> to a type declaration that includes the STRONG phrase". 13.18.57.4 GR1: "The effect of the TYPE
      *> clause is as though the data description identified by type-name-1 had been coded in place of the
      *> TYPE clause", so A is as though 05 A USAGE PROGRAM-POINTER were written in the ordinary group G,
      *> exactly as 05 Q SAME AS a level-1 pointer is (13.18.49.4 GR1, the same words); WP inside the WEAK
      *> type declaration W is the template spelling of the same placement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3129NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT IS TYPEDEF USAGE PROGRAM-POINTER.
       01 DT IS TYPEDEF USAGE POINTER.
       01 W IS TYPEDEF.
          05 WP TYPE DT.
       01 G.
          05 A TYPE PT.
          05 X PIC X(3).
       PROCEDURE DIVISION.
           STOP RUN.
