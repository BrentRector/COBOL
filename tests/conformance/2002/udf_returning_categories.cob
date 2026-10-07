      *> ISO 8.4.3.2.4 GR1 / 14.2.2 SR5 / 14.8.3 — category-carrying FUNCTION-ID RETURNING: the caller-side
      *> result temp clones the callee's FULL RETURNING description (category + width + edited mask), so an
      *> ALPHANUMERIC result MOVEs/DISPLAYs/compares as text and folds its cloned width under FUNCTION LENGTH,
      *> a GROUP result crosses the activation boundary as its character image (group MOVE + child access +
      *> direct DISPLAY), a NUMERIC-EDITED result carries its edited mask image, and a NATIONAL result rides
      *> the national category channel (MOVE to PIC N + national relation).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UFALNP10UR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC X(2).
       01 L-R PIC X(8).
       PROCEDURE DIVISION USING L-A RETURNING L-R.
       P.
           MOVE L-A TO L-R
           MOVE "*" TO L-R(3:1)
           GOBACK.
       END FUNCTION UFALNP10UR.

      *> Group RETURNING: an alphanumeric child + a numeric-DISPLAY child cross as one character image.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UFGRPP10UR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-G.
          05 L-G-A PIC X(3).
          05 L-G-N PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-G.
       P.
           MOVE "GRP" TO L-G-A
           COMPUTE L-G-N = L-X * 3
           GOBACK.
       END FUNCTION UFGRPP10UR.

      *> Numeric-edited RETURNING: the ZZ,ZZ9 mask image is the carried result.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UFEDTP10UR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-V PIC 9(5).
       01 L-E PIC ZZ,ZZ9.
       PROCEDURE DIVISION USING L-V RETURNING L-E.
       P.
           MOVE L-V TO L-E
           GOBACK.
       END FUNCTION UFEDTP10UR.

      *> National RETURNING: NATIONAL-OF widens the argument; the N(4) result pads with national spaces.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UFNATP10UR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC X(2).
       01 L-NR PIC N(4).
       PROCEDURE DIVISION USING L-A RETURNING L-NR.
       P.
           MOVE FUNCTION NATIONAL-OF(L-A) TO L-NR
           GOBACK.
       END FUNCTION UFNATP10UR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UFCATP10UR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UFALNP10UR.
           FUNCTION UFGRPP10UR.
           FUNCTION UFEDTP10UR.
           FUNCTION UFNATP10UR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T PIC X(8).
       01 WS-L PIC 9(2).
       01 WS-G.
          05 WS-G-A PIC X(3).
          05 WS-G-N PIC 9(4).
       01 WS-N PIC N(4).
       PROCEDURE DIVISION.
       MAIN.
      *>   The alphanumeric leg: MOVE / direct DISPLAY / LENGTH fold / relation over the cloned X(8) temp.
           MOVE FUNCTION UFALNP10UR("AB") TO WS-T
           DISPLAY "A=" WS-T "<"
           DISPLAY "D=" FUNCTION UFALNP10UR("XY") "<"
           COMPUTE WS-L = FUNCTION LENGTH(FUNCTION UFALNP10UR("QQ"))
           DISPLAY "L=" WS-L
           IF FUNCTION UFALNP10UR("OK") = "OK*"
               DISPLAY "R=Y"
           END-IF
      *>   The group leg: group MOVE (image transfer), receiver child access, direct DISPLAY (AsImage).
           MOVE FUNCTION UFGRPP10UR(7) TO WS-G
           DISPLAY "G=" WS-G
           DISPLAY "G2=" FUNCTION UFGRPP10UR(2)
           DISPLAY "GA=" WS-G-A " GN=" WS-G-N
      *>   The numeric-edited leg: the edited mask image displayed and moved to an alphanumeric receiver.
           DISPLAY "E=" FUNCTION UFEDTP10UR(12345)
           MOVE FUNCTION UFEDTP10UR(60) TO WS-T
           DISPLAY "E2=" WS-T "<"
      *>   The national leg: MOVE to PIC N (national pad) + a national relation.
           IF FUNCTION UFNATP10UR("CD") = N"CD  "
               DISPLAY "NR=Y"
           END-IF
           MOVE FUNCTION UFNATP10UR("AB") TO WS-N
           DISPLAY "N=" WS-N "<"
           STOP RUN.
       END PROGRAM UFCATP10UR.

      *> Alphanumeric RETURNING: PIC X(8), left-justified store + a ref-mod splice in the callee.
