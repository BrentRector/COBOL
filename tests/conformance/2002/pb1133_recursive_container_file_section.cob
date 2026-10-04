      *> kb/Work PB1133 - A RECURSIVE PROGRAM THAT CONTAINS PROGRAMS MAY DECLARE A FILE SECTION.
      *>   8.6.4: "Data items and file connectors defined in the working-storage or file section of a source element
      *>     that is not an initial program are static items", so FCONT1133's record area FREC, its FILE STATUS item
      *>     FS and its counter PASSNO are ONE copy each (13.5.4 GR1; last-used on re-activation, 14.6.2.3.3).
      *>   8.4.6.2.2: a file-name described with a GLOBAL clause is a global name, and 13.18.27.4 GR2: every global
      *>     name of a container is
      *>     referenceable in the programs it contains without describing them again - FW1133 (directly contained)
      *>     writes the container's record area and reads the container's FS, FW2 (contained in FW1133, two levels
      *>     out) adds to the container's PASSNO. 12.4.5.8.4 GR1 NOTE 1: the status of an I/O statement on a GLOBAL
      *>     file in a contained program is stored in the file-status item of the program that describes the file.
      *>   Derived trace: pass 1 - OPEN OUTPUT 00; FS is set to ZZ, then FW1133's WRITE must store 00 into the
      *>     container's FS (a store anywhere else would leave ZZ); PASSNO 1, FW2 adds 10 -> 11; CLOSE; OPEN INPUT;
      *>     READ returns the record FW1133 wrote; pass 2 continues from the last-used 11 -> 12 -> 22 and leaves F
      *>     OPEN (an internal connector is last-used too, 14.6.2.3.3). CANCEL (14.9.5 GR9 closes the open internal
      *>     connector; 14.9.5 GR3 + 14.6.2.3.2 case 3 and action 3 put the data in the initial state with the
      *>     connector in no open mode): pass 3 starts from PASSNO 0 -> 1 -> 11, and its OPEN OUTPUT answers 00, which
      *>     it would not (41) on a connector still open.
      *>   Each leg can fail: refused outright, FW1133's WRITE lands in a private record area (READ shows spaces),
      *>     its status lands in a private copy (AFTER shows ZZ), or FW2's ADD does not reach the one PASSNO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1133F.
       PROCEDURE DIVISION.
           CALL "FCONT1133"
           CALL "FCONT1133"
           CANCEL "FCONT1133"
           CALL "FCONT1133"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FCONT1133 RECURSIVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1133f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F GLOBAL.
       01 FREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX GLOBAL.
       01 PASSNO PIC 99 VALUE 0 GLOBAL.
       PROCEDURE DIVISION.
           ADD 1 TO PASSNO
           OPEN OUTPUT F
           DISPLAY "OPEN " FS
           MOVE "ZZ" TO FS
           CALL "FW1133"
           DISPLAY "AFTER " FS
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "READ " FS " " FREC
           IF PASSNO < 20
               CLOSE F
           ELSE
               DISPLAY "LEFT OPEN"
           END-IF
           DISPLAY "PASS " PASSNO
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FW1133.
       PROCEDURE DIVISION.
           MOVE "ABCD" TO FREC
           WRITE FREC
           DISPLAY "WR " FS
           CALL "FW2"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FW2.
       PROCEDURE DIVISION.
           ADD 10 TO PASSNO
           GOBACK.
       END PROGRAM FW2.
       END PROGRAM FW1133.
       END PROGRAM FCONT1133.
       END PROGRAM PB1133F.
