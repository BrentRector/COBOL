      *> reject-at: 2014 2023
      *> kb/Work PB1213 — ISO/IEC 1989:2023 §13.18.5.3 SR2: "The subject of the entry shall not be a dynamic-length
      *> elementary item or a variable-length group." G is a variable-length group (§8.5.1.12.1: a dynamic-length
      *> elementary item, D, is subordinate to it). Before the fix it compiled and printed A=ABC D=HELLO, because
      *> the BASED cell had learned to CARRY a dynamic-length leaf and nothing asked the syntax rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1213BV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G BASED.
          05 A PIC X(3).
          05 GS.
             10 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
           ALLOCATE G.
           MOVE "ABC" TO A.
           MOVE "HELLO" TO D.
           DISPLAY "A=" A " D=" D.
           STOP RUN.
