      *> kb/Work PB2496 - A DYNAMIC-CAPACITY TABLE OF VARIABLE-LENGTH ELEMENTS
      *> ACROSS A FORMAT-2 CALL BOUNDARY.
      *>
      *> 14.9.4.3 SR25 applies 14.8.2: "If either the formal parameter or the
      *> argument is a variable length group, the formal parameter and the
      *> argument shall be compatible, as described in 8.5.1.12". G and L are of
      *> one shape; A and LB are compatible but of DIFFERENT shapes: each element
      *> of TA holds a dynamic-capacity table IA where TB's element holds a fixed
      *> one-occurrence table IB at the same relative byte position (8.5.1.12.3
      *> sentence 3), elements of one byte length.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   S  the callee sees G whole (the first call)          = hab1c2zz, cap 2
      *>   R1 14.2.3 GR8: BY REFERENCE the callee's stores are the caller's:
      *>      DL (2) := "XYZ"; DL (3) creates occurrence 3 (8.5.1.9.3) = N, 3
      *>                                                       = hab1XYZ2N3zz, 3
      *>   S/R2 BY CONTENT (14.2.3 GR9): the callee works on a copy, G unchanged
      *>   S3 LB sees A in its own shape: IB's one occurrence is IA (1, 1)
      *>                                                       = a1pza, cap 1
      *>   R3 IB (1, 1) := "w" is written OVER IA's current occurrences, never
      *>      re-sizing them, so IA (1, 2) "q" survives; DB (2) := "new" creates
      *>      TB (2), which reaches A as TA (2) with IA of one space
      *>                                                       = a1wqnew za, 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2496CAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 G.
          05 H PIC X VALUE "h".
          05 TD OCCURS DYNAMIC CAPACITY IN CAP FROM 1.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
          05 Z PIC X DYNAMIC LENGTH LIMIT 4.
       01 A.
          05 TA OCCURS DYNAMIC CAPACITY IN CA FROM 1.
             10 DA PIC X DYNAMIC LENGTH LIMIT 5.
             10 IA PIC X OCCURS DYNAMIC CAPACITY IN CIA FROM 1.
          05 ZA PIC X(2) VALUE "za".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ab" TO DD (1)
           MOVE "1" TO FX (1)
           MOVE "c" TO DD (2)
           MOVE "2" TO FX (2)
           MOVE "zz" TO Z
           CALL "PB2496SR" AS NESTED USING BY REFERENCE G
           MOVE CAP TO WS-C
           DISPLAY "R1=" WS-C " [" G "]"
           CALL "PB2496SR" AS NESTED USING BY CONTENT G
           MOVE CAP TO WS-C
           DISPLAY "R2=" WS-C " [" G "]"
           MOVE "a1" TO DA (1)
           MOVE "p" TO IA (1, 1)
           MOVE "q" TO IA (1, 2)
           CALL "PB2496SB" AS NESTED USING A
           MOVE CA TO WS-C
           DISPLAY "R3=" WS-C " [" A "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2496SR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L PIC 9.
       LINKAGE SECTION.
       01 L.
          05 H PIC X.
          05 TL OCCURS DYNAMIC CAPACITY IN CL FROM 1.
             10 DL PIC X DYNAMIC LENGTH LIMIT 5.
             10 FL PIC X.
          05 ZL PIC X DYNAMIC LENGTH LIMIT 4.
       PROCEDURE DIVISION USING L.
           MOVE CL TO WS-L
           DISPLAY "S=" WS-L " [" L "]"
           MOVE "XYZ" TO DL (2)
           MOVE "N" TO DL (3)
           MOVE "3" TO FL (3)
           GOBACK.
       END PROGRAM PB2496SR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2496SB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L PIC 9.
       LINKAGE SECTION.
       01 LB.
          05 TB OCCURS DYNAMIC CAPACITY IN CB FROM 1.
             10 DB PIC X DYNAMIC LENGTH LIMIT 5.
             10 IB PIC X OCCURS 1.
          05 ZB PIC X(2).
       PROCEDURE DIVISION USING LB.
           MOVE CB TO WS-L
           DISPLAY "S3=" WS-L " [" LB "]"
           MOVE "w" TO IB (1, 1)
           MOVE "new" TO DB (2)
           GOBACK.
       END PROGRAM PB2496SB.
       END PROGRAM PB2496CAL.
