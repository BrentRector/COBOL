      *> kb/Work PB1133 - A RECURSIVE PROGRAM THAT CONTAINS PROGRAMS OWNS STATIC WORKING-STORAGE, AND ITS CONTAINEES
      *>   REACH ITS GLOBAL DATA THROUGH THAT STATIC COPY.
      *>   13.5.4 GR1: the working-storage of a program that does not have the initial attribute is STATIC data (one
      *>     copy in the run unit); 14.6.2.3.3: static data is in the LAST-USED state when the program is activated
      *>     again; 14.6.2.3.2 case 3: it is placed in the INITIAL state "after the execution of a CANCEL statement
      *>     referencing the program"; 11.10.4 GR4: the RECURSIVE clause makes the program AND ANY PROGRAMS CONTAINED
      *>     WITHIN IT recursive, so INNER's working-storage is static too; 13.18.27.4 GR2: a contained program may
      *>     reference a global name of its container without describing it again.
      *>   Derived trace (every row computed from those rules, not from a run):
      *>     call 1: CTR 1, GCTR 11, GTAB(2) is 0, GBASE is AB; INNER: ICTR 1, GCTR 16, GTAB(2) 7, GBASE XY.
      *>     call 2 (last-used): CTR 2, GCTR 17, GTAB(2) still 7, GBASE still XY; INNER: ICTR 2, GCTR 22.
      *>     CANCEL "CONT1133" (case 3, and by 14.9.5 GR4 over the program it contains): everything back to initial.
      *>     call 3: CTR 1, GCTR 11, GTAB(2) 0 and GBASE AB again, INNER's ICTR 1 again.
      *>   Each leg can fail: the program is refused outright, or INNER's GCTR / GTAB / GBASE write lands in a private
      *>   copy (C pre-INNER shows the container's own value), or CANCEL leaves a static member behind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1133OK.
       PROCEDURE DIVISION.
           CALL "CONT1133"
           CALL "CONT1133"
           CANCEL "CONT1133"
           CALL "CONT1133"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONT1133 RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CTR PIC 99 VALUE 0.
       01 GCTR PIC 99 VALUE 10 GLOBAL.
       01 GTAB GLOBAL.
          05 TE PIC 9 OCCURS 3 INDEXED BY TI VALUE 0.
       01 GBASE PIC XX VALUE "AB" GLOBAL.
       01 GB REDEFINES GBASE PIC XX.
       PROCEDURE DIVISION.
           ADD 1 TO CTR
           ADD 1 TO GCTR
           DISPLAY "C CTR=" CTR " G=" GCTR " PRE=" TE (2) " " GB
           CALL "INNER1133"
           DISPLAY "C POST G=" GCTR " TE=" TE (2) " " GB
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER1133.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ICTR PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO ICTR
           ADD 5 TO GCTR
           SET TI TO 2
           MOVE 7 TO TE (TI)
           MOVE "XY" TO GBASE
           DISPLAY "I ICTR=" ICTR " G=" GCTR
           GOBACK.
       END PROGRAM INNER1133.
       END PROGRAM CONT1133.
       END PROGRAM PB1133OK.
