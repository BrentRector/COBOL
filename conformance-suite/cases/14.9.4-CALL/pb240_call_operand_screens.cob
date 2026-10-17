*>--- conformance-case
*> id: pb240_call_operand_screens
*> rules: [SR-14.9.4.3-6, SR-14.9.4.3-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB240 - the two 14.9.4.3 operand screens, on their LEGAL
      *> side. Each CALL below is conforming source.
      *>
      *> 1. SR6 through a REDEFINES view: VB2 starts at bit 8 of W - the
      *>    redefinition "starts at the first bit of the data item referenced
      *>    by data-name-2" (13.18.44.4 GR1) and VB1's 8 bits fill the first byte
      *>    (8.5.1.6.3) - so it IS "aligned on a byte boundary" and crosses
      *>    BY REFERENCE; the callee's MOVE lands in W's second byte through
      *>    14.2.3 GR8 ("the same storage area"): VB2=10101010 before,
      *>    01010101 after, and VB1 untouched: 11110000.
      *> 2. ANY LENGTH at a Format-2 CALL. SR11 bans it in FORMAT 1 and
      *>    SR18 bans identifier-4 (BY CONTENT / BY VALUE) in Format 2; a
      *>    Format-2 BY REFERENCE identifier-2 is instead subject to
      *>    14.8.2.3.2, whose rule d) makes an ANY LENGTH formal match any
      *>    length ("its length is considered to match the length of the
      *>    corresponding argument") and rule e) lets an ANY LENGTH argument
      *>    meet an ANY LENGTH formal. PB240IN receives X (6 characters) as
      *>    its ANY LENGTH L (13.18.2.4 GR1b - six repetitions): L=ABCDEF;
      *>    forwards L BY REFERENCE AS NESTED to PB240LF's ANY LENGTH M:
      *>    M=ABCDEF; whose MOVE ALL "Z" fills the six positions of X's
      *>    storage: X=ZZZZZZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240MN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "ABCDEF".
       01 W PIC X(2).
       01 V REDEFINES W.
          05 VB1 PIC 1(8) USAGE BIT.
          05 VB2 PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"11110000" TO VB1
           MOVE B"10101010" TO VB2
           DISPLAY "VB2=" VB2
           CALL "PB240BT" AS NESTED USING BY REFERENCE VB2
           DISPLAY "VB2=" VB2
           DISPLAY "VB1=" VB1
           CALL "PB240IN" AS NESTED USING BY REFERENCE X
           DISPLAY "X=" X
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240BT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FB PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING BY REFERENCE FB.
       MAIN.
           MOVE B"01010101" TO FB
           GOBACK.
       END PROGRAM PB240BT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240IN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE L.
       MAIN.
           DISPLAY "L=" L
           CALL "PB240LF" AS NESTED USING BY REFERENCE L
           GOBACK.
       END PROGRAM PB240IN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240LF COMMON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M PIC X ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE M.
       MAIN.
           DISPLAY "M=" M
           MOVE ALL "Z" TO M
           GOBACK.
       END PROGRAM PB240LF.
       END PROGRAM PB240MN.
