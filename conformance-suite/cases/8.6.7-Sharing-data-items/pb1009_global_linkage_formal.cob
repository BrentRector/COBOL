*>--- conformance-case
*> id: pb1009_global_linkage_formal
*> rules: [GR-8.6.7-3, SR-13.18.27.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1009 -- a contained program references its container's
      *> GLOBAL LINKAGE formals and the container's other global
      *> residences.  ISO 13.18.27.3 SR1 b) admits GLOBAL on a level-1
      *> entry "specified in the file, working-storage, local-storage, or
      *> linkage section"; 13.18.27.4 GR2: a contained program "may
      *> reference that name without describing it again" -- the storage
      *> stays the container's, so every store the contained program makes
      *> is seen by the container and, through the BY REFERENCE formals
      *> (14.2.3 GR8), by the main program.
      *> The contained program has its OWN first formal (Z) -- it must not
      *> alias the container's first formal (X).
      *> A GLOBAL constant-name (13.18.27.4 GR1 names constant-names)
      *> is visible in the contained program too, as a literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1009M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4) VALUE "AAAA".
       01 N PIC 9(3) VALUE 12.
       01 R.
          05 R1 PIC X(2) VALUE "RR".
       PROCEDURE DIVISION.
           CALL "PB1009C" USING A N R
           DISPLAY "M A=" A " N=" N " R=" R
           STOP RUN.
       END PROGRAM PB1009M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1009C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WB PIC X(4) VALUE "WBWB".
       01 GW PIC X(4) VALUE "GWGW" GLOBAL.
       01 P USAGE POINTER GLOBAL.
       01 BB PIC X(4) BASED GLOBAL.
       01 K CONSTANT IS GLOBAL AS 42.
       LOCAL-STORAGE SECTION.
       01 LS PIC X(3) VALUE "LSV" GLOBAL.
       LINKAGE SECTION.
       01 X PIC X(4) GLOBAL.
       01 Y PIC 9(3) GLOBAL.
       01 G GLOBAL.
          05 G1 PIC X(2).
       PROCEDURE DIVISION USING X Y G.
           SET ADDRESS OF BB TO ADDRESS OF WB
           CALL "PB1009N" USING WB
           DISPLAY "C X=" X " Y=" Y " G=" G " BB=" BB " LS=" LS
           DISPLAY "C GW=" GW
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1009N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q USAGE POINTER.
       01 KT PIC 9(3) VALUE K.
       LINKAGE SECTION.
       01 Z PIC X(4).
       PROCEDURE DIVISION USING Z.
           DISPLAY "N Z=" Z " X=" X " Y=" Y " G1=" G1 " BB=" BB
               " LS=" LS
           DISPLAY "N K=" K " KT=" KT
           MOVE "XNEW" TO X
           ADD 1 TO Y
           MOVE "GG" TO G1
           MOVE "BNEW" TO BB
           MOVE "LN" TO LS
      *>   ADDRESS OF a global name, taken in the contained program,
      *>   addresses the CONTAINER's storage (8.4.3.11.4 GR1).
           SET P TO ADDRESS OF GW
           SET ADDRESS OF BB TO P
           MOVE "B1B1" TO BB
           SET Q TO ADDRESS OF BB
           IF Q = P DISPLAY "N SAME ADDRESS" END-IF
           GOBACK.
       END PROGRAM PB1009N.
       END PROGRAM PB1009C.
