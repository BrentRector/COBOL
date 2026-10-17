      *>--- conformance-case
      *> id: pb164_comp5_group_image
      *> rules: [GR-14.9.11.4-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB164GI.
      *> kb/Work PB164 wave 1 - the image predicate on the ONE ByteForm
      *> table: COMP-5 and BINARY-CHAR..DOUBLE leaves have a PINNED
      *> radix-2 big-endian byte form of StorageWidth bytes (A.1 items
      *> 205/208/215), so a group containing them IS image-capable. The
      *> DISCRIMINATING legs (the review fleet caught a same-shape group
      *> MOVE riding the memberwise fast path that never consults the
      *> predicate):
      *> X - MOVE G TO XB then XB TO G2 forces AsImage AND FromImage
      *> through the alphanumeric intermediary (14.9.25.4 GR4: a group
      *> move "is treated exactly as if it were an alphanumeric to
      *> alphanumeric elementary move ... no conversion" - the bytes
      *> travel, and each leaf decodes its own bytes back). W - the
      *> same round trip for an UNSIGNED 8-byte COMP-5 carrier (ulong;
      *> the codec's unsigned lanes, whose absence made the generated
      *> C# uncompilable). A - CALL BY REFERENCE: "the activated
      *> runtime element operates as if the formal parameter occupies
      *> the same storage area as the argument" (14.2.3 GR8), so the
      *> callee's writes to BOTH leaves are the caller's.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 C5 PIC 9(4) COMP-5 VALUE 513.
          05 BS USAGE BINARY-SHORT VALUE 258.
       01 G2.
          05 C5-2 PIC 9(4) COMP-5.
          05 BS-2 USAGE BINARY-SHORT.
       01 XB PIC X(4).
       01 GW.
          05 W10 PIC 9(10) COMP-5 VALUE 9876543210.
          05 WF PIC X(2) VALUE "ok".
       01 GW2.
          05 W10-2 PIC 9(10) COMP-5.
          05 WF-2 PIC X(2).
       01 XW PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           MOVE G TO XB
           MOVE XB TO G2
           DISPLAY "X=[" C5-2 " " BS-2 "]"
           MOVE GW TO XW
           MOVE XW TO GW2
           DISPLAY "W=[" W10-2 " " WF-2 "]"
           CALL "SUB164GI" USING G
           DISPLAY "A=[" C5 " " BS "]"
           STOP RUN.
       END PROGRAM PB164GI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB164GI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LC5 PIC 9(4) COMP-5.
          05 LBS USAGE BINARY-SHORT.
       PROCEDURE DIVISION USING LG.
       MAIN.
           ADD 1 TO LC5
           ADD 1 TO LBS
           GOBACK.
       END PROGRAM SUB164GI.
