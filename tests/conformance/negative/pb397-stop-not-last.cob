*> reject-at: 85 2002 2014 2023
*> kb/Work PB397 - ISO/IEC 1989:2023 14.9.42.3 SR1: "The STOP statement shall be specified only as the last
*> statement in any discreet block of code." No edition qualifier, so all four editions refuse. The block here is
*> the THEN phrase of an IF (docs/CONFORMANCE.md D-SEQ reads a "discreet block" as a consecutive sequence of
*> imperative statements): STOP RUN is followed by a DISPLAY in the same phrase, which can never execute.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB397NEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           IF WS-A = 1 STOP RUN DISPLAY "IN-IF" END-IF.
