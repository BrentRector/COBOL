      *> kb/Work PB1190 -- a WRITE with an ADVANCING phrase on a RECORD
      *> VARYING ... DEPENDING file is bound by the VARYING clause like
      *> any other WRITE.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "shall not be
      *>   larger than the largest or smaller than the smallest number
      *>   of bytes allowed by the RECORD IS VARYING clause"
      *>                                          -> OK  §14.9.51.4 14)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   execution of a WRITE statement is unsuccessful, the write
      *>   operation does not take place"         -> OK  §14.9.51.4 15)
      *> VF's records are 3 to 8 bytes and the DEPENDING item VL names
      *> each record's length (§13.18.43.4 GR13 a)). VL = 9 is larger
      *> than 8 and VL = 2 smaller than 3, so both ADVANCING writes are
      *> unsuccessful with '44' and write nothing (GR14, GR15); the
      *> print-control arm used to skip the bound and write the whole
      *> 8-byte area with '00'. VL = 5 is inside the bounds, so the plain
      *> WRITE succeeds and writes a 5-byte record: the file then holds
      *> exactly one record, which READ delivers with VL = 5 (GR15 of
      *> §13.18.43.4) -- the two refused writes left nothing behind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1190VA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT VF ASSIGN TO "pb1190va.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD VF RECORD IS VARYING IN SIZE FROM 3 TO 8 CHARACTERS
             DEPENDING ON VL.
       01 VR PIC X(8).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 VL PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT VF
           MOVE ALL "X" TO VR
           MOVE 9 TO VL
           WRITE VR AFTER ADVANCING 1 LINE
           DISPLAY "AFTER-VL9=" FS
           MOVE 2 TO VL
           WRITE VR BEFORE ADVANCING 1 LINE
           DISPLAY "BEFORE-VL2=" FS
           MOVE 5 TO VL
           WRITE VR
           DISPLAY "PLAIN-VL5=" FS
           CLOSE VF
           OPEN INPUT VF
           READ VF
           DISPLAY "READ1=" FS " VL=" VL " REC=" VR(1:VL)
           READ VF
           DISPLAY "READ2=" FS
           CLOSE VF
           STOP RUN.
