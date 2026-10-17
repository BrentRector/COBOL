*>--- conformance-case
*> id: pb244-pointer-group-transfer-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB244 - the edition floor under conformance:cases/14.9.11-DISPLAY/pb244_pointer_group_transfer. The strongly-
      *> typed group (TYPEDEF STRONG, 13.18.58) and USAGE POINTER (13.18.60) are COBOL-2002 introductions, and
      *> 13.18.60.3 SR14 admits a pointer leaf only under a STRONG type declaration, so at COBOL-85 the group
      *> the positive case displays cannot be declared at all; COBOLNET0900 is the version gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244PTRGRP85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GPT IS TYPEDEF STRONG.
          05 GA PIC X(3).
          05 GP USAGE POINTER.
          05 GS OCCURS 2.
             10 GSN PIC 9(2).
             10 GSP USAGE PROGRAM-POINTER.
          05 GZ PIC X(2).
       01 WS-GP TYPE GPT.
       01 WS-X PIC X(4) VALUE "wxyz".
       01 WS-PX USAGE POINTER.
       01 WS-DST PIC X(40).
       01 WS-D2.
          05 WS-D2A PIC X(5).
          05 WS-D2B PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO GA OF WS-GP
           MOVE 12 TO GSN OF WS-GP (1)
           MOVE 34 TO GSN OF WS-GP (2)
           MOVE "zz" TO GZ OF WS-GP
           SET WS-PX TO ADDRESS OF WS-X
           SET GP OF WS-GP TO WS-PX
           DISPLAY "[" WS-GP "]"
           DISPLAY "[" GS OF WS-GP (2) "]"
           MOVE WS-GP TO WS-DST
           DISPLAY "{" WS-DST "}"
           MOVE WS-GP TO WS-D2
           DISPLAY "<" WS-D2 ">"
           DISPLAY FUNCTION LENGTH (WS-GP)
           STOP RUN.
