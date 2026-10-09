      *> kb/Work PB2465 (train 1047 review): a constant's length phrase completes its operand's description
      *> AHEAD of the pipeline (ISO §13.10.4: BYTE-LENGTH OF E is E's completed size, 6 under GROUP-USAGE
      *> NATIONAL), but a SAME AS clause naming that entry still copies it as WRITTEN (§13.18.49.4 GR1;
      *> GR3 transfers a USAGE only from a group to which the SUBJECT is subordinate), so S is PIC 9(3)
      *> DISPLAY, 3 bytes, whichever entry the constant measured first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2465SA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN GROUP-USAGE NATIONAL.
          05 E PIC 9(3).
       01 KE CONSTANT AS BYTE-LENGTH OF E OF GN.
       01 KG CONSTANT AS BYTE-LENGTH OF GN.
       01 S SAME AS E.
       01 T SAME AS GN.
       PROCEDURE DIVISION.
           DISPLAY "KE=" KE " KG=" KG
           DISPLAY "S=" FUNCTION BYTE-LENGTH(S)
                   " E=" FUNCTION BYTE-LENGTH(E OF GN)
                   " T=" FUNCTION BYTE-LENGTH(T)
           STOP RUN.
