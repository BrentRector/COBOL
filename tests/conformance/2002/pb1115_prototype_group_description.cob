      *> kb/Work PB1115, row SR-13.7.3-2 -- ISO 13.7.3 SR2: "The
      *> description of the formal parameters and the returning item that
      *> appear in the linkage section of a function prototype or a
      *> program prototype shall match the description of the formal
      *> parameters and the returning item in the corresponding function
      *> definition or program definition" (with 10.6.2 SR2: the
      *> signatures shall be the same; 8.13 stores "the description of
      *> the parameters" as the signature). A GROUP formal's description
      *> is its subordinate entries: 8.5.3.1 states when two group
      *> descriptions describe the same data -- each elementary item at the
      *> same relative position, of the same length, with the same ALIGNED,
      *> BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN,
      *> SYNCHRONIZED and USAGE clauses; intermediate GROUPING and the
      *> data-names are not among them.
      *> The prototype P1GQ declares 01 L-G (05 PIC 9(2), 05 PIC X(2)); the
      *> definition P1GQDEF AS "P1GQ" declares 01 D-G whose intermediate
      *> group D-S holds the PIC 9(2) and whose PIC X(2) follows it -- the
      *> same elementary layout under other names and another grouping, so
      *> the descriptions match and the program compiles (the negative
      *> pb1115-prototype-group-subordinates swaps the two fields).
      *> DERIVATION of the output. PB1G calls P1GQ (12.3.8.4 GR10 a): the
      *> in-group definition with the externalized name "P1GQ" is the
      *> program called) BY REFERENCE with W-G = "07" "XY". The definition
      *> displays its view of the group, A=07 B=XY, then adds 5 to D-A and
      *> moves "ZZ" to D-B; BY REFERENCE shares the storage, so the caller
      *> sees W-A=12 W-B=ZZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1GQ IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G.
          05 L-A PIC 9(2).
          05 L-B PIC X(2).
       PROCEDURE DIVISION USING L-G.
       END PROGRAM P1GQ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P1GQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-A PIC 9(2) VALUE 7.
          05 W-B PIC X(2) VALUE "XY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL P1GQ USING W-G
           DISPLAY "W-A=" W-A " W-B=" W-B
           STOP RUN.
       END PROGRAM PB1G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1GQDEF AS "P1GQ".
       DATA DIVISION.
       LINKAGE SECTION.
       01 D-G.
          03 D-S.
             05 D-A PIC 9(2).
          03 D-B PIC X(2).
       PROCEDURE DIVISION USING D-G.
       P-MAIN.
           DISPLAY "A=" D-A " B=" D-B
           ADD 5 TO D-A
           MOVE "ZZ" TO D-B
           GOBACK.
       END PROGRAM P1GQDEF.
