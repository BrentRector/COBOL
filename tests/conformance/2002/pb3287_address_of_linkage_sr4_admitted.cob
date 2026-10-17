      *> kb/Work PB3287 - ADDRESS OF a LINKAGE item the procedure division MAY reference.
      *> ISO 8.4.3.11.3 SR1 admits a data item of the linkage section as identifier-1, and 13.7.3 SR4 says which linkage
      *> items the procedure division may reference at all: "A based data item may be referenced as described in 13.18.5,
      *> BASED clause; otherwise, a data item defined in the linkage section of a source element may be referenced within
      *> the procedure division of that source element if, and only if, it satisfies one of the following conditions:
      *> a) It is an operand of the USING phrase ...  b) It is subordinate to an operand of the USING phrase ...
      *> c) It is defined with a REDEFINES or RENAMES clause, the object of which satisfies one of the above conditions.
      *> d) It is subordinate to any item that satisfies the condition in subrule c."
      *> ADDRESS OF is such a reference, so the screen that refuses the unreferenceable record (the negative
      *> pb3287-address-of-linkage-not-referenceable) must admit every one of these, or the fix would refuse legal source.
      *> The five legs below each take the address of one admitted shape and show it is THE address, by storing through
      *> a BASED item set at it; 8.4.3.11.4 GR1: the result "contains the address of identifier-1".
      *>   a) L  (USING operand)               - SET P1 ... ; P1 and P2 are the same address (c redefines a)
      *>   c) LR (REDEFINES L)                 - SET P2 ...
      *>   d) LR2 (second half of LR)          - two bytes past P1: MOVE "ZZ" there makes L "ABZZ"
      *>   b) LG2 (second half of USING group) - MOVE "99" there makes LG "PQ99"
      *>   BASED) LB                           - ADDRESS OF a BASED item is its own pointer's referent: P5 = P1 once set
      *> Main shows the caller's storage, because a USING operand occupies the argument's storage (14.2.3 GR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "ABCD".
       01 G.
          05 G1 PIC X(2) VALUE "PQ".
          05 G2 PIC X(2) VALUE "RS".
       PROCEDURE DIVISION.
           CALL "PB3287S" USING A G
           DISPLAY "MAIN A=" A " G=" G
           STOP RUN.
       END PROGRAM PB3287M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 USAGE POINTER.
       01 P2 USAGE POINTER.
       01 P3 USAGE POINTER.
       01 P4 USAGE POINTER.
       01 P5 USAGE POINTER.
       01 B1 PIC X(2) BASED.
       01 B2 PIC X(2) BASED.
       LINKAGE SECTION.
       01 L PIC X(4).
       01 LR REDEFINES L.
          05 LR1 PIC X(2).
          05 LR2 PIC X(2).
       01 LG.
          05 LG1 PIC X(2).
          05 LG2 PIC X(2).
       01 LB PIC X(4) BASED.
       PROCEDURE DIVISION USING L LG.
           SET P1 TO ADDRESS OF L
           SET P2 TO ADDRESS OF LR
           IF P1 = P2 DISPLAY "SUB SAME=Y"
           ELSE DISPLAY "SUB SAME=N" END-IF
           SET ADDRESS OF LB TO P1
           SET P5 TO ADDRESS OF LB
           IF P5 = P1 DISPLAY "SUB BASED=Y"
           ELSE DISPLAY "SUB BASED=N" END-IF
           DISPLAY "SUB LB=" LB
           SET P3 TO ADDRESS OF LR2
           SET ADDRESS OF B1 TO P3
           MOVE "ZZ" TO B1
           SET P4 TO ADDRESS OF LG2
           SET ADDRESS OF B2 TO P4
           MOVE "99" TO B2
           DISPLAY "SUB L=" L " LG=" LG
           GOBACK.
       END PROGRAM PB3287S.
