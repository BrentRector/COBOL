      *> ISO 13.7.3 SR4 a)-e): every linkage item the procedure division MAY reference.
      *> SR4: "a data item defined in the linkage section of a source element may be
      *>   referenced within the procedure division of that source element if, and only
      *>   if, it satisfies one of the following conditions: a) It is an operand of the
      *>   USING phrase or the RETURNING phrase of the procedure division header. b) It
      *>   is subordinate to an operand of the USING phrase ... c) It is defined with a
      *>   REDEFINES or RENAMES clause, the object of which satisfies one of the above
      *>   conditions. d) It is subordinate to any item that satisfies the condition in
      *>   subrule c. e) It is a condition-name or index-name associated with a data
      *>   item that satisfies one of the above conditions."
      *>   cite.py --check 13.7.3 -> OK 13.7.3 4)
      *> Each leg below is referenced (a negative twin per leg is in negative/pb1249-*):
      *>   a) L-A (USING operand)         b) L-G1, L-GT (under USING operand L-G)
      *>   c) L-A2 REDEFINES L-A, L-REN RENAMES L-G1 THRU L-G2
      *>   d) L-GR1 (under L-GR, which REDEFINES L-G)
      *>   e) L-A-ON (condition-name of L-A), L-GIX (index-name of L-GT)
      *> DERIVATION: the caller passes A = 0007 and G = "ABCDEFGH" + "QQ".
      *>   L-A-ON (VALUE 7) is true => ON; L-A2 is L-A's storage => 0007; L-G1 => ABCD;
      *>   L-REN spans L-G1 thru L-G2 => ABCDEFGH; L-GR1 is L-G's storage => ABCDEFGH;
      *>   SET L-GIX TO 2 selects the second L-GT element => Q.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC 9(4) VALUE 7.
       01  G.
           05  G1  PIC X(4) VALUE "ABCD".
           05  G2  PIC X(4) VALUE "EFGH".
           05  GT  PIC X OCCURS 2 VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1249B" USING A G.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249B.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A     PIC 9(4).
           88  L-A-ON VALUE 7.
       01  L-A2    REDEFINES L-A PIC X(4).
       01  L-G.
           05  L-G1 PIC X(4).
           05  L-G2 PIC X(4).
           05  L-GT PIC X OCCURS 2 INDEXED BY L-GIX.
       66  L-REN   RENAMES L-G1 THRU L-G2.
       01  L-GR    REDEFINES L-G.
           05  L-GR1 PIC X(8).
       PROCEDURE DIVISION USING L-A L-G.
       SUB-P.
           IF L-A-ON DISPLAY "ON".
           DISPLAY L-A2.
           DISPLAY L-G1.
           DISPLAY L-REN.
           DISPLAY L-GR1.
           SET L-GIX TO 2.
           DISPLAY L-GT (L-GIX).
           EXIT PROGRAM.
       END PROGRAM PB1249B.
       END PROGRAM PB1249A.
