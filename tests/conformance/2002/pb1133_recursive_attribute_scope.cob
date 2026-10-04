      *> kb/Work PB1133 - THE RECURSIVE ATTRIBUTE, INHERITED BY CONTAINED PROGRAMS THAT OWN WORKING-STORAGE AND
      *>   CONTAIN PROGRAMS, AND WHAT IT DOES AND DOES NOT MAKE VISIBLE.
      *>   11.10.4 GR4: "The RECURSIVE clause specifies that the program and any programs contained within it are
      *>     recursive. The program may be called while it is active and may call itself. If the RECURSIVE clause is
      *>     not specified in a program or implied for a program, the program shall not be called while it is active."
      *>   8.4.6.3 1): a program directly contained in C (here XC in XR) that is not COMMON may be referenced only by
      *>     statements in that containing program or, if it possesses the recursive attribute, in the program itself.
      *>   XC carries no RECURSIVE clause: it inherits the attribute from XR, owns working-storage (static, 13.5.4 GR1,
      *>     one copy across the three activations) and contains XC1, so every shape the refused composition forbade is
      *>     in this one program. 14.9.4.4 GR3 f): a non-recursive program called while it is active raises
      *>     EC-PROGRAM-RECURSIVE-CALL (NR2 calls NR1 back).
      *>   Derived trace: XR calls XC; XC activations 1, 2 and 3 (self-calls, N static: 1 2 3); the third calls XC1;
      *>     XC1 is contained IN XC, so it is neither XC's container nor XC itself - XC is not visible to it (the
      *>     exception path runs). Then NR1 calls NR2, which calls NR1 while NR1 is active: refused.
      *>   Each leg can fail: the program is refused outright, XC's self-call is refused or its N restarts, XC1 reaches
      *>     XC, or NR2's call re-enters NR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1133S.
       PROCEDURE DIVISION.
           CALL "XR"
           CALL "NR1"
           STOP RUN.
       END PROGRAM PB1133S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XR RECURSIVE.
       PROCEDURE DIVISION.
           DISPLAY "XR"
           CALL "XC"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO N
           DISPLAY "XC " N
           IF N < 3
               CALL "XC"
           ELSE
               CALL "XC1"
           END-IF
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XC1.
       PROCEDURE DIVISION.
           CALL "XC"
               ON EXCEPTION DISPLAY "XC1 NO XC"
           END-CALL
           GOBACK.
       END PROGRAM XC1.
       END PROGRAM XC.
       END PROGRAM XR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NR1.
       PROCEDURE DIVISION.
           DISPLAY "NR1"
           CALL "NR2"
           GOBACK.
       END PROGRAM NR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NR2.
       PROCEDURE DIVISION.
           CALL "NR1"
               ON EXCEPTION DISPLAY "NR2 RECURSIVE CALL REFUSED"
           END-CALL
           GOBACK.
       END PROGRAM NR2.
