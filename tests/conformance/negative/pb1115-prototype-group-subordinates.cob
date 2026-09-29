      *> reject-at: 2002 2014 2023
      *> kb/Work PB1115, row SR-13.7.3-2 -- ISO 13.7.3 SR2: "The
      *> description of the formal parameters and the returning item that
      *> appear in the linkage section of a function prototype or a
      *> program prototype shall match the description of the formal
      *> parameters and the returning item in the corresponding function
      *> definition or program definition" (10.6.2 SR2: "the signatures of
      *> these two compilation units shall be the same"). The prototype's
      *> group L-G is 05 PIC 9(2) then 05 PIC X(2); the definition's is
      *> PIC X(2) then PIC 9(2). Both groups are four character positions
      *> wide, so a width compare cannot tell them apart, but the
      *> subordinate entries -- which ARE a group's description (8.5.3.1:
      *> the elementary items' positions, lengths and clauses) -- differ:
      *> COBOLNET1513. Before the fix this compiled and the definition read
      *> the caller's "07XY" as D-A="07" (alphanumeric) and D-B="XY" as a
      *> PIC 9(2) field.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1GS IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G.
          05 L-A PIC 9(2).
          05 L-B PIC X(2).
       PROCEDURE DIVISION USING L-G.
       END PROGRAM P1GS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1GN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P1GS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-A PIC 9(2) VALUE 7.
          05 W-B PIC X(2) VALUE "XY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL P1GS USING W-G
           STOP RUN.
       END PROGRAM PB1GN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1GSDEF AS "P1GS".
       DATA DIVISION.
       LINKAGE SECTION.
       01 D-G.
          05 D-A PIC X(2).
          05 D-B PIC 9(2).
       PROCEDURE DIVISION USING D-G.
       P-MAIN.
           DISPLAY "A=" D-A " B=" D-B
           GOBACK.
       END PROGRAM P1GSDEF.
