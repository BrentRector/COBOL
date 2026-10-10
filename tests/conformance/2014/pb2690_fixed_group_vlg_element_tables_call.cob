      *> kb/Work PB2690 - A FIXED-LENGTH GROUP OPPOSITE A DYNAMIC-CAPACITY TABLE
      *> OF VARIABLE-LENGTH ELEMENTS, ACROSS A FORMAT-2 CALL BOUNDARY, IN BOTH
      *> DIRECTIONS AND AS A RETURNING ITEM.
      *>
      *> 14.8.2.2 2): "If either the formal parameter or the argument is a
      *> variable length group, the formal parameter and the argument shall be
      *> compatible, as described in 8.5.1.12" (14.8.3.2 says the same of the
      *> RETURNING pair), and 8.5.1.12.1 admits a fixed-length group on the
      *> other side. FA holds a fixed table
      *> TF of two elements, each a one-character DF and a fixed table FI of two
      *> characters; L and VG hold a dynamic-capacity table whose element is a
      *> one-character item and a dynamic-capacity table. The elements
      *> correspond (8.5.1.12.3 sentence 2, the byte lengths both 3) and a fixed
      *> table is treated as a dynamic-capacity table of its fixed occurrence
      *> count (sentence 3); 14.6.9.2 moves correspondingly numbered elements
      *> by the rules of the MOVE statement, "if the sending table has a higher
      *> current capacity than the receiving table, superfluous elements are not
      *> moved" and a missing element is space filled.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   S1  the callee sees FA whole, capacity 2 (the fixed table's count)
      *>                                                      = habcdef, 2
      *>   R1  14.2.3 GR8: BY REFERENCE the callee's stores are the caller's:
      *>       IL (2, 1) := "X" is FA's second element's first FI occurrence;
      *>       DL (2) := "Y" is its DF                        = habcYXf
      *>   S2/R2 BY CONTENT (14.2.3 GR9): the callee works on a copy
      *>                                                      = habcYXf
      *>   V0  VG: h, [a b c d] [e f] [g h i]                 = habcdefghi, 3
      *>   S3  the fixed formal sees the first two elements, each IL cut to
      *>       two occurrences ("d" superfluous, the third element not moved)
      *>                                                      = habcef_
      *>   R3  GR8 again: LD (1) := "Q" and LI (1, 1) := "R" are VG's first
      *>       element and its first IL occurrence; the occurrences the formal
      *>       cannot see keep their values and the capacity stays 3
      *>                                                      = hQRcdefghi, 3
      *>   S4/R4 BY CONTENT: the callee sees the changed VG and VG is unchanged
      *>   RT1 14.9.4.4 GR4 with 14.8.3.2: a variable-length RETURNING item
      *>       delivered to a fixed group, cut as S3 is       = habcef_
      *>   RT2 the fixed RETURNING item delivered to a variable-length group
      *>       takes capacity 2                               = habcdef, 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690CAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 FA.
          05 FH PIC X VALUE "h".
          05 TF OCCURS 2.
             10 DF PIC X.
             10 FI PIC X OCCURS 2.
       01 FB.
          05 FBH PIC X VALUE "-".
          05 FBT OCCURS 2.
             10 FBD PIC X VALUE "-".
             10 FBI PIC X OCCURS 2 VALUE "-".
       01 VG.
          05 VH PIC X VALUE "h".
          05 VT OCCURS DYNAMIC CAPACITY IN CV FROM 1.
             10 VD PIC X.
             10 VI PIC X OCCURS DYNAMIC CAPACITY IN CVI FROM 1.
       01 VR.
          05 VRH PIC X VALUE "-".
          05 VRT OCCURS DYNAMIC CAPACITY IN CVR FROM 1.
             10 VRD PIC X.
             10 VRI PIC X OCCURS DYNAMIC CAPACITY IN CVRI FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "a" TO DF (1)
           MOVE "b" TO FI (1, 1)
           MOVE "c" TO FI (1, 2)
           MOVE "d" TO DF (2)
           MOVE "e" TO FI (2, 1)
           MOVE "f" TO FI (2, 2)
           CALL "PB2690SR" AS NESTED USING BY REFERENCE FA
           DISPLAY "R1=[" FA "]"
           CALL "PB2690SR" AS NESTED USING BY CONTENT FA
           DISPLAY "R2=[" FA "]"
           MOVE "a" TO VD (1)
           MOVE "b" TO VI (1, 1)
           MOVE "c" TO VI (1, 2)
           MOVE "d" TO VI (1, 3)
           MOVE "e" TO VD (2)
           MOVE "f" TO VI (2, 1)
           MOVE "g" TO VD (3)
           MOVE "h" TO VI (3, 1)
           MOVE "i" TO VI (3, 2)
           MOVE CV TO WS-C
           DISPLAY "V0=" WS-C " [" VG "]"
           CALL "PB2690FX" AS NESTED USING BY REFERENCE VG
           MOVE CV TO WS-C
           DISPLAY "R3=" WS-C " [" VG "]"
           CALL "PB2690FX" AS NESTED USING BY CONTENT VG
           MOVE CV TO WS-C
           DISPLAY "R4=" WS-C " [" VG "]"
           CALL "PB2690RV" AS NESTED RETURNING FB
           DISPLAY "RT1=[" FB "]"
           CALL "PB2690RF" AS NESTED RETURNING VR
           MOVE CVR TO WS-C
           DISPLAY "RT2=" WS-C " [" VR "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L PIC 9.
       LINKAGE SECTION.
       01 L.
          05 LH PIC X.
          05 TL OCCURS DYNAMIC CAPACITY IN CL FROM 1.
             10 DL PIC X.
             10 IL PIC X OCCURS DYNAMIC CAPACITY IN CIL FROM 1.
       PROCEDURE DIVISION USING L.
           MOVE CL TO WS-L
           DISPLAY "S=" WS-L " [" L "]"
           MOVE "X" TO IL (2, 1)
           MOVE "Y" TO DL (2)
           GOBACK.
       END PROGRAM PB2690SR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690FX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF.
          05 LFH PIC X.
          05 LFT OCCURS 2.
             10 LD PIC X.
             10 LI PIC X OCCURS 2.
       PROCEDURE DIVISION USING LF.
           DISPLAY "S=[" LF "]"
           MOVE "Q" TO LD (1)
           MOVE "R" TO LI (1, 1)
           GOBACK.
       END PROGRAM PB2690FX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690RV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV.
          05 RHX PIC X.
          05 RTX OCCURS DYNAMIC CAPACITY IN CR FROM 1.
             10 RDX PIC X.
             10 RIX PIC X OCCURS DYNAMIC CAPACITY IN CRI FROM 1.
       PROCEDURE DIVISION RETURNING RV.
           MOVE "h" TO RHX
           MOVE "a" TO RDX (1)
           MOVE "b" TO RIX (1, 1)
           MOVE "c" TO RIX (1, 2)
           MOVE "d" TO RIX (1, 3)
           MOVE "e" TO RDX (2)
           MOVE "f" TO RIX (2, 1)
           MOVE "g" TO RDX (3)
           MOVE "h" TO RIX (3, 1)
           GOBACK.
       END PROGRAM PB2690RV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2690RF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 XF.
          05 XH PIC X.
          05 XT OCCURS 2.
             10 XD PIC X.
             10 XI PIC X OCCURS 2.
       PROCEDURE DIVISION RETURNING XF.
           MOVE "h" TO XH
           MOVE "a" TO XD (1)
           MOVE "b" TO XI (1, 1)
           MOVE "c" TO XI (1, 2)
           MOVE "d" TO XD (2)
           MOVE "e" TO XI (2, 1)
           MOVE "f" TO XI (2, 2)
           GOBACK.
       END PROGRAM PB2690RF.
       END PROGRAM PB2690CAL.
