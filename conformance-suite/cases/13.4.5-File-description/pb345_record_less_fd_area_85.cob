      *>--- conformance-case
      *> id: pb345_record_less_fd_area_85
      *> rules: [SR-13.4.5.3-3, GR-14.9.30.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The COBOL-85 twin of 2023/pb345_record_less_fd_area (kb/Work PB345). ISO/IEC
      *> 1989:2023 §13.4.5.3 SR3's permission to write a file description entry with NO
      *> record description entries, and §14.9.30.4 GR6's implied "alphanumeric group item
      *> of the maximum size established by the RECORD clause", carry no edition gate --
      *> docs/VERSION_CHANGE_REFERENCE.md records no delta for either -- so the SAME
      *> program behaves identically at --std 85. This twin exists to keep it that way:
      *> gating the record-less FD by accident would be invisible at 2023 alone.
      *> The UNLOCK legs of the 2023 twin are ABSENT here on purpose -- the UNLOCK
      *> statement is a COBOL-2002 introduction (§14.9.47), gated at bind.
      *>
      *> DERIVATION (identical to the 2023 twin, same clauses) --
      *>   OPEN=00  §9.1.13.2 item 1.
      *>   R1       the implied 20-byte area to a PIC X(20) receiver: same bytes.
      *>   R2       the implied entry is a GROUP, so §14.9.25.4 GR4 second paragraph makes
      *>            the implicit MOVE an alphanumeric-to-alphanumeric elementary move and
      *>            the leftmost 5 of "12345ABCDEFGHIJKLMNO" reach the PIC 9(5) receiver.
      *>   R3=10    §9.1.13.4 item 1 a).
      *>   RD1=47   READ on a CLOSED connector: §9.1.13.7 item 7.
      *>   OO=00 / CL=00   successful OPEN OUTPUT and CLOSE (§9.1.13.2 item 1).
      *>   RD2=23   RANDOM READ of relative record 1 of an empty file: §9.1.13.5 item 3 a).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345R85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FOUT ASSIGN TO "pb345r85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-OUT.
           SELECT FIN ASSIGN TO "pb345r85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-IN.
           SELECT RLF ASSIGN TO "pb345l85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS-RL.
       DATA DIVISION.
       FILE SECTION.
       FD  FOUT.
       01  OREC PIC X(20).
       FD  FIN RECORD CONTAINS 20 CHARACTERS.
       FD  RLF RECORD CONTAINS 5 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  FS-OUT PIC XX.
       01  FS-IN  PIC XX.
       01  FS-RL  PIC XX.
       01  RK     PIC 9(4).
       01  BUF    PIC X(20).
       01  NUMBUF PIC 9(5).
       01  RBUF   PIC X(5).
       PROCEDURE DIVISION.
           OPEN OUTPUT FOUT
           MOVE "ABCDEFGHIJKLMNOPQRST" TO OREC
           WRITE OREC
           MOVE "12345ABCDEFGHIJKLMNO" TO OREC
           WRITE OREC
           CLOSE FOUT
           OPEN INPUT FIN
           DISPLAY "OPEN=" FS-IN
           READ FIN INTO BUF
               AT END DISPLAY "UNEXPECTED-AT-END-1"
           END-READ
           DISPLAY "R1=" FS-IN " [" BUF "]"
           MOVE 99999 TO NUMBUF
           READ FIN INTO NUMBUF
               AT END DISPLAY "UNEXPECTED-AT-END-2"
           END-READ
           DISPLAY "R2=" FS-IN " [" NUMBUF "]"
           READ FIN INTO BUF
               AT END DISPLAY "R3=" FS-IN
           END-READ
           CLOSE FIN
           MOVE 1 TO RK
           MOVE "ZZ" TO FS-RL
           READ RLF INTO RBUF
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "RD1=" FS-RL
           OPEN OUTPUT RLF
           DISPLAY "OO=" FS-RL
           CLOSE RLF
           DISPLAY "CL=" FS-RL
           OPEN I-O RLF
           MOVE 1 TO RK
           MOVE "ZZ" TO FS-RL
           READ RLF INTO RBUF
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "RD2=" FS-RL
           CLOSE RLF
           STOP RUN.
