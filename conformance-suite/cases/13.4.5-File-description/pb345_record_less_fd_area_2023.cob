*>--- conformance-case
*> id: pb345_record_less_fd_area_2023
*> rules: [SR-13.4.5.3-3, SR-13.4.5.3-7, GR-13.4.5.4-1, SR-13.18.43.3-1,
*>    GR-14.9.30.4-6, GR-14.9.47.4-2, GR-14.9.47.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §13.4.5.3 SR3 -- "When no record description entries are
      *> specified: a) a RECORD clause shall be specified in the file description entry,
      *> ... c) an INTO phrase shall be specified on all READ statements associated with
      *> the file." SR7 withdraws that permission only "For an indexed file", so BOTH of
      *> the record-description-less file description entries below -- SEQUENTIAL FIN and
      *> RELATIVE RLF -- are LEGAL source.
      *> §14.9.30.4 GR6 says what such a file's record area IS: "The execution of a READ
      *> statement with the INTO phrase when there are no record description entries
      *> subordinate to the file description entry proceeds as though there were one
      *> record description entry describing an alphanumeric group item of the maximum
      *> size established by the RECORD clause."
      *> kb/Work PB345: before it, NEITHER organization registered a file connector for
      *> such an entry and the first I-O verb aborted the run unit.
      *>
      *> DERIVATION of every line of the .out --
      *>   OPEN=00  §9.1.13.2 item 1: "The input-output statement is successfully executed
      *>            and no further information is available concerning the input-output
      *>            operation."
      *>   R1       the implied 20-byte area moved to a PIC X(20) receiver: same bytes.
      *>   R2       the implied entry is a GROUP item, so §14.9.25.4 GR4 second paragraph
      *>            governs the implicit MOVE -- "Any move that is not an elementary move
      *>            ... is treated exactly as if it were an alphanumeric to alphanumeric
      *>            elementary move" -- and the leftmost 5 characters of
      *>            "12345ABCDEFGHIJKLMNO" land in the PIC 9(5) receiver: 12345. Were the
      *>            implied entry an ELEMENTARY alphanumeric item instead, GR4's FIRST
      *>            paragraph would make this an elementary alphanumeric-to-numeric move
      *>            of all 20 characters, which are not a numeric value at all. This line
      *>            is what pins GR6's word "group".
      *>   R3=10    §9.1.13.4 item 1 a): "A sequential READ statement is attempted and no
      *>            next or prior logical record exists in the physical file because
      *>            a) NEXT was specified or implied and the end of the physical file has
      *>            been reached".
      *>   U1=42    UNLOCK on a CLOSED connector. §14.9.47.4 GR2 -- "File-name-1 shall
      *>            reference a file connector in the open mode." -- is unmet, GR3 --
      *>            "The execution of the UNLOCK statement causes the value of the I-O
      *>            status of the file connector referenced by file-name-1 to be updated."
      *>            -- still applies, and §9.1.13.7 item 2 names the value: "I-O status =
      *>            42. A CLOSE or UNLOCK statement is attempted for a file connector that
      *>            is not in an open mode." RS is seeded "ZZ" first, so 42 is measured
      *>            and not left over.
      *>   RD1=47   READ on a CLOSED connector: §9.1.13.7 item 7, "The execution of a READ
      *>            or START statement is attempted referencing a file connector that is
      *>            not open in the input or I-O mode."
      *>   OO=00 / CL=00   successful OPEN OUTPUT and CLOSE (§9.1.13.2 item 1).
      *>   U2=00    UNLOCK on an OPEN connector: §14.9.47.4 GR1 -- "The presence or
      *>            absence of any record locks does not affect the success of the
      *>            execution of the UNLOCK statement."
      *>   RD2=23   a RANDOM READ of relative record 1 of the empty file just created:
      *>            §9.1.13.5 item 3 a), "an attempt is made to randomly access a record
      *>            that does not exist in the physical file".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345RA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FOUT ASSIGN TO "pb345ra.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-OUT.
           SELECT FIN ASSIGN TO "pb345ra.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-IN.
           SELECT RLF ASSIGN TO "pb345rl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS-RL.
       DATA DIVISION.
       FILE SECTION.
       FD  FOUT.
       01  OREC PIC X(20).
      *> No record description entry -- §13.4.5.3 SR3, and the RECORD clause SR3 a) demands.
       FD  FIN RECORD CONTAINS 20 CHARACTERS.
      *> The relative twin: also no record description entry (SR7 exempts only INDEXED).
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
           MOVE "ZZ" TO FS-RL.
           UNLOCK RLF.
           DISPLAY "U1=" FS-RL
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
           MOVE "ZZ" TO FS-RL.
           UNLOCK RLF.
           DISPLAY "U2=" FS-RL
           MOVE 1 TO RK
           MOVE "ZZ" TO FS-RL
           READ RLF INTO RBUF
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "RD2=" FS-RL
           CLOSE RLF
           STOP RUN.
