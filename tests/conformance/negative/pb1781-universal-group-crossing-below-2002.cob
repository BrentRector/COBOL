*> reject-at: 85
      *> kb/Work PB1781 - the NEGATIVE twin of tests/conformance/2002/pb1781_universal_group_crossing.
      *> Object orientation - CLASS-ID, USAGE OBJECT REFERENCE and the INVOKE statement (ISO 13.18.60, 14.9.23) - is
      *> a COBOL-2002 introduction that does not exist in COBOL-85, so the program is rejected at --std 85, gated by
      *> COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1781NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U USAGE OBJECT REFERENCE.
       01  G.
           05  G1          PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           INVOKE U "TG" USING G
           STOP RUN.
