*> reject-at: 2002 2014
*> kb/Work PB1079 - the edition floor of the positive golden
*> 2023/w68r_pb1079_external_entry_identity: the file control entry identity check
*> (ISO 14.8.4.4 -> 12.4.5.3 GR1) raises EC-EXTERNAL-FILE-MISMATCH, and the EC-EXTERNAL
*> family is new in COBOL-2023 (Annex E.2 item 9) - enabling it by >>TURN at 2002/2014
*> is COBOLNET0878, never a silent no-op. (At --std 85 the >>TURN directive itself is
*> refused first, COBOLNET0900 - pinned by the turn-directive registry row.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68RNEG1.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "w68r-neg1.dat"
               ORGANIZATION INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK
               RESERVE 3 AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD XF IS EXTERNAL.
       01 XREC.
           05 XK PIC X(4).
           05 XD PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "RAN"
           STOP RUN.
