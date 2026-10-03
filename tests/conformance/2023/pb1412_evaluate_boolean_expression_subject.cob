      *> kb/Work PB1412 - THE STILL-OPEN HALF (PENDING). ISO 1989:2023 14.9.13.2 prints boolean-expression-1 as a
      *> selection subject and Table 15 (14.9.13.3 SR10) has a Boolean-expression column; GR3 d) "Any selection
      *> subject in which boolean-expression-1 is specified is assigned a boolean value according to the rules for
      *> evaluating boolean expressions". The object B"1000" is a boolean expression too (Table 15: Boolean
      *> expression x Boolean expression is 'Y'), compared as boolean-expression-2.
      *>   cite.py --check 14.9.13.4 "Any selection subject in which boolean-expression-1 is specified is assigned
      *>     a boolean value according to the rules for evaluating boolean expressions." -> OK 3) d)
      *> A = 1100, C = 1010. A B-AND C = 1000, equal to B"1000": EV1-Y. A B-SHIFT-L 1 = 1000: EV2-Y. A B-OR C =
      *> 1110, not B"1000": EV3-N. Today each draws COBOLNET1511 + COBOLNET1634 (the subject is parsed as a
      *> condition and held to the simple-boolean-condition length-1 rule). Move to 'enabled' when it lands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412EVB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       01 C PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE A B-AND C
              WHEN B"1000" DISPLAY "EV1-Y"
              WHEN OTHER DISPLAY "EV1-N"
           END-EVALUATE.
           EVALUATE A B-SHIFT-L 1
              WHEN B"1000" DISPLAY "EV2-Y"
              WHEN OTHER DISPLAY "EV2-N"
           END-EVALUATE.
           EVALUATE A B-OR C
              WHEN B"1000" DISPLAY "EV3-Y"
              WHEN OTHER DISPLAY "EV3-N"
           END-EVALUATE.
           STOP RUN.
