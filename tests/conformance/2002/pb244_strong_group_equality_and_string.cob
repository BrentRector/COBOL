      *> ISO/IEC 1989:2023 8.8.4.2.12 and 14.9.43.3 SR1 (kb/Work PB244): the legs of a strongly-typed group operand that
      *> an IMAGE cannot express.
      *> 8.8.4.2.12: when two strongly-typed group items are compared, each elementary item of the first operand is
      *> compared with the corresponding elementary item of the second, in order, and the operands are equal when all
      *> pairs are equal. A POINTER leaf is an elementary item compared as a pointer, so two groups that differ only
      *> in where their pointers point are UNEQUAL - a reserved-placeholder image of the group would call them equal.
      *> 14.9.43.3 SR1: a strongly-typed group whose elementary items are all usage display is described as usage
      *> display, and is a legal STRING sender (the negative pb244-string-strong-group-pointer-leaf is the group
      *> holding a pointer).
      *> WHY EACH LINE CAN FAIL:
      *>   EQ1   same text, both pointers at WS-X: every pair equal.
      *>   NE2   same text, pointers at different items: the pointer pair is unequal.
      *>   NE3   NOT = is the complement of NE2.
      *>   NE4   differing GZ with equal pointers: the last pair is unequal.
      *>   S=    [abczz]  STRING of a strongly-typed group of display items.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244EQSTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GPT IS TYPEDEF STRONG.
          05 GA PIC X(3).
          05 GP USAGE POINTER.
          05 GZ PIC X(2).
       01 GDT IS TYPEDEF STRONG.
          05 DA PIC X(3).
          05 DZ PIC X(2).
       01 WS-GP TYPE GPT.
       01 WS-GQ TYPE GPT.
       01 WS-DG TYPE GDT.
       01 WS-X PIC X(4) VALUE "wxyz".
       01 WS-Y PIC X(4) VALUE "wxyz".
       01 WS-PX USAGE POINTER.
       01 WS-PY USAGE POINTER.
       01 WS-DST PIC X(10) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO GA OF WS-GP
           MOVE "zz" TO GZ OF WS-GP
           MOVE "abc" TO GA OF WS-GQ
           MOVE "zz" TO GZ OF WS-GQ
           SET WS-PX TO ADDRESS OF WS-X
           SET WS-PY TO ADDRESS OF WS-Y
           SET GP OF WS-GP TO WS-PX
           SET GP OF WS-GQ TO WS-PX
           IF WS-GP = WS-GQ DISPLAY "EQ1" ELSE DISPLAY "NE1" END-IF
           SET GP OF WS-GQ TO WS-PY
           IF WS-GP = WS-GQ DISPLAY "EQ2" ELSE DISPLAY "NE2" END-IF
           IF WS-GP NOT = WS-GQ DISPLAY "NE3" ELSE DISPLAY "EQ3" END-IF
           SET GP OF WS-GQ TO WS-PX
           MOVE "k" TO GZ OF WS-GQ
           IF WS-GP = WS-GQ DISPLAY "EQ4" ELSE DISPLAY "NE4" END-IF
           MOVE "abc" TO DA OF WS-DG
           MOVE "zz" TO DZ OF WS-DG
           STRING WS-DG DELIMITED BY SIZE INTO WS-DST
           DISPLAY "S=[" WS-DST(1:5) "]"
           STOP RUN.
