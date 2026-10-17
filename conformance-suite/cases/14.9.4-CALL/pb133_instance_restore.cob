*>--- conformance-case
*> id: pb133_instance_restore
*> rules: [GR-14.9.4.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 - the registry's instance slot must be RESTORED when a nested activation ends:
      *> contained programs reach their container's frame through it, so after IR(d2) returns, a containee
      *> activated by the still-running IR(d1) must alias d1's automatic data (14.6.2.3.2 - LOCAL-STORAGE
      *> is per-activation), not the dead d2 frame's. LS-X is GLOBAL level-1 LOCAL-STORAGE (13.18.27.3
      *> SR1b - kb/Work PB163 made the LS section's GLOBAL entries register at all), read by the contained
      *> PB133IC. Derived trace: d1 tags its frame LS-X=1, recurses; d2 tags LS-X=2, activates IC ->
      *> C-SEES=0002; d2 returns (slot restored to the d1 frame); d1 activates IC -> C-SEES=0001. The old
      *> runtime left the d2 frame in the slot and printed 0002 twice.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB133M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB133IR" USING WS-D
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB133IR RECURSIVE.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LS-X GLOBAL PIC 9(4) VALUE 0.
       01 D2 PIC 9.
       LINKAGE SECTION.
       01 D PIC 9.
       PROCEDURE DIVISION USING D.
       P.
           MOVE D TO LS-X
           IF D < 2
               COMPUTE D2 = D + 1
               CALL "PB133IR" USING D2
           END-IF
           CALL "PB133IC"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB133IC.
       PROCEDURE DIVISION.
       Q.
           DISPLAY "C-SEES=" LS-X
           GOBACK.
       END PROGRAM PB133IC.
       END PROGRAM PB133IR.
       END PROGRAM PB133M.
