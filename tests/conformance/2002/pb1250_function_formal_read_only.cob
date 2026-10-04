      *> ISO 13.7.3 SR5: "A formal parameter of a function shall not be used as a receiving
      *>   operand." (cite.py --check 13.7.3 "A formal parameter of a function shall not be
      *>   used as a receiving operand" -> OK 13.7.3 5))
      *> POSITIVE HALF: the function READS its formal parameters (P-X, P-G), stores into its
      *>   RETURNING item P-R (not a formal parameter) and into its own WORKING-STORAGE copy.
      *> DERIVATION: LKDB5(N, G) with N = 10 and G = "AB": W-COPY = P-X = 10; ADD 1 => 11;
      *>   P-R = W-COPY * 2 = 22 (00022); the caller's N and G are untouched (10, AB).
      *>   NEGATIVES: negative/pb1250-*.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. LKDB5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-COPY  PIC 9(4).
       01  W-TXT   PIC XX.
       LINKAGE SECTION.
       01  P-X     PIC 9(4).
       01  P-G.
           05  P-G1 PIC X(2).
       01  P-R     PIC 9(5).
       PROCEDURE DIVISION USING P-X P-G RETURNING P-R.
           MOVE P-X TO W-COPY.
           ADD 1 TO W-COPY.
           MOVE P-G1 TO W-TXT.
           COMPUTE P-R = W-COPY * 2.
           GOBACK.
       END FUNCTION LKDB5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1250A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION LKDB5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N       PIC 9(4) VALUE 10.
       01  G       PIC XX VALUE "AB".
       01  R       PIC 9(5).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R = FUNCTION LKDB5 (N G).
           DISPLAY R " " N " " G.
           STOP RUN.
       END PROGRAM PB1250A.
