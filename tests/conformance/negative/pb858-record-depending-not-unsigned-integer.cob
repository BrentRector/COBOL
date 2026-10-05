      *> reject-at: 85 2002 2014 2023
      *> RECORD ... DEPENDING ON A GROUP, A SIGNED ITEM AND A NON-INTEGER (kb/Work PB858).
      *> ISO/IEC 1989:2023 §13.18.43.3 SR6: "Data-name-1 shall describe an elementary
      *> unsigned integer in the working-storage, local-storage, or linkage section."
      *> Each item is in working-storage, so only the first two obligations are asked:
      *> W-GRP is a group (not elementary), W-SGN is signed, W-DEC has a decimal place.
      *> Each is refused, COBOLNET2922.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB858DU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb858du1.dat".
           SELECT G ASSIGN TO "pb858du2.dat".
           SELECT H ASSIGN TO "pb858du3.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD IS VARYING IN SIZE FROM 1 TO 20
           DEPENDING ON W-GRP.
       01 F-REC PIC X(20).
       FD G RECORD IS VARYING IN SIZE FROM 1 TO 20
           DEPENDING ON W-SGN.
       01 G-REC PIC X(20).
       FD H RECORD IS VARYING IN SIZE FROM 1 TO 20
           DEPENDING ON W-DEC.
       01 H-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01 W-GRP.
          05 W-A PIC 99.
       01 W-SGN PIC S99.
       01 W-DEC PIC 9V9.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
