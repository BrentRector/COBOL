      *>--- conformance-case
      *> id: stepd_fd_multi01_comp
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STEPDFD.
      *> THE STEP D LATENT BLAST RADIUS, PINNED (the design scout: 507
      *> MULTI-01 FD/SD CONSTRUCTS CORPUS-WIDE, NONE WITH A NON-DISPLAY
      *> LEAF): records[1..] IMPLICITLY REDEFINE records[0] (9.1.2 -
      *> D.6.3.4.3: "ALL ASSOCIATED RECORD DESCRIPTION ENTRIES ARE
      *> REDEFINITIONS OF THE SAME STORAGE AREA"), SO A COMP LEAF IN A
      *> SECONDARY RECORD IS A TIER-B BYTE WINDOW OVER THE SHARED AREA.
      *> WRITE THROUGH THE COMP-CARRYING RECORD, OBSERVE THE BYTES
      *> THROUGH THE X RECORD (FUNCTION ORD - 04 D2 = 1234), AND READ
      *> THE FILE BACK THROUGH THE X VIEW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "stepd-fd.dat"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-R1 PIC X(5).
       01 F-R2.
          05 F-N PIC 9(4) COMP.
          05 F-X PIC X(3).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE 1234 TO F-N
           MOVE "xyz" TO F-X
           DISPLAY "A=" FUNCTION ORD(F-R1(1:1)) " "
               FUNCTION ORD(F-R1(2:1)) " " F-R1(3:3)
           WRITE F-R2
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "B=" FUNCTION ORD(F-R1(1:1)) " "
               FUNCTION ORD(F-R1(2:1)) " " F-R1(3:3) " " FS
           CLOSE F
           STOP RUN.
