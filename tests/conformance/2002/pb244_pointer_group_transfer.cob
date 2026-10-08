      *> kb/Work PB244 shape (a) - a strongly-typed group with a class pointer leaf is a legal DISPLAY operand
      *> (ISO 1989:2023 14.9.11.3 SR1 bars only an identifier referencing a data item of class message-tag, object
      *> or pointer; a strongly-typed group's class is its type-name, 8.5.2.1) and a legal MOVE sender (14.9.25.3
      *> SR2 constrains only a strongly-typed RECEIVER). 14.9.11.4 GR1 leaves the device conversion to the
      *> implementor: CONFORMANCE.md A.1 item 56 - the group's storage image, each pointer leaf as its 8 storage
      *> positions holding the pointer's storage image (A.1 item 216, kb/Work PB1071: NULL is the zero address; it
      *> was 8 reserved spaces before), never the managed reference. Width = 3 + 8 + 2 x (2 + 8) + 2 = 33.
      *> The image bytes of a non-NULL pointer are the implementor's (13.18.60.4 GR23) and are control characters,
      *> so this program moves the group to alphanumeric receivers (the same one-way image DISPLAY writes) and
      *> shows the characters around the pointer leaves, asking of each leaf's 8 positions only what the
      *> determination fixes: NULL = LOW-VALUES, a set pointer is not.
      *>   R1 = {abc|12|34|zz|       }   MOVE to PIC X(40): 33 characters + 7 spaces (GR4)
      *>   R2 = <abc|12|34|zz|       >    MOVE to a 45-position alphanumeric group: 33 characters + 12 spaces
      *>   R3 = 33                        FUNCTION LENGTH agrees with the image width
      *> Before the fix these MOVEs compiled and aborted at run time with the Tier-C "pointer/object-class leaf" loud.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244PTRGRP.
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
       01 WS-D2 PIC X(45).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO GA OF WS-GP
           MOVE 12 TO GSN OF WS-GP (1)
           MOVE 34 TO GSN OF WS-GP (2)
           MOVE "zz" TO GZ OF WS-GP
           SET WS-PX TO ADDRESS OF WS-X
           SET GP OF WS-GP TO WS-PX
           MOVE WS-GP TO WS-DST
           DISPLAY "{" WS-DST(1:3) "|" WS-DST(12:2) "|" WS-DST(22:2)
               "|" WS-DST(32:2) "|" WS-DST(34:7) "}"
           IF WS-DST(4:8) = LOW-VALUES
              DISPLAY "GP=NULL-IMAGE"
           ELSE
              DISPLAY "GP=ADDRESS-IMAGE"
           END-IF
           IF WS-DST(14:8) = LOW-VALUES AND WS-DST(24:8) = LOW-VALUES
              DISPLAY "GSP=NULL-IMAGE"
           ELSE
              DISPLAY "GSP=ADDRESS-IMAGE"
           END-IF
           MOVE WS-GP TO WS-D2
           DISPLAY "<" WS-D2(1:3) "|" WS-D2(12:2) "|" WS-D2(22:2) "|"
               WS-D2(32:2) "|" WS-D2(34:12) ">"
           SET GP OF WS-GP TO NULL
           MOVE WS-GP TO WS-DST
           IF WS-DST(4:8) = LOW-VALUES
              DISPLAY "GP-AFTER-NULL=NULL-IMAGE"
           ELSE
              DISPLAY "GP-AFTER-NULL=ADDRESS-IMAGE"
           END-IF
           DISPLAY FUNCTION LENGTH (WS-GP)
           STOP RUN.
