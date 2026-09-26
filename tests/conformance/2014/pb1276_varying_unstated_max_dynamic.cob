      *> kb/Work PB1276 -- the unstated maximum of a variable-length file is ONE
      *> quantity, whether the RECORD clause is written without integer-3 or implied.
      *> Until PB1276 the explicit clause took the record AREA's character width,
      *> which leaves out a DYNAMIC LENGTH member, while the implied clause took the
      *> descriptions' maximum: F2 and F3 below answered 44 to the record F4 wrote.
      *>
      *> RULES (each run through scripts/spec/cite.py --check) --
      *>   13.18.43.4 GR10   "If integer-3 is not specified, the maximum number of
      *>                     bytes to be contained in any record of the file is
      *>                     equal to the greatest number of bytes described for a
      *>                     record in that file".
      *>   13.18.43.4 GR8 b) "The maximum number of table elements described in the
      *>                     record is used"; a dynamic-length item counts at its
      *>                     maximum size (8.5.1.10.1): LIMIT 20 = 20 bytes.
      *>   13.18.43.4 GR14 a) a record outside [integer-2, integer-3]: status 44
      *>                     (9.1.13.7 4)).
      *>
      *> DERIVATION -- every record here is A (3 bytes) + D (at most 20) = 23 bytes
      *> at its greatest, so GR10's maximum is 23 for all three files.
      *>   F2 (explicit clause, DEPENDING ON L): L=17 is in range: F2-17=00; L=23 is
      *>        the maximum: F2-23=00; L=24 exceeds it: F2-24=44.
      *>   F3 (explicit clause, no DEPENDING): the record's own 17 bytes (GR13 b):
      *>        F3=00.
      *>   F4 (no RECORD clause; D-FRA (iv) implies Format 2): F4=00 -- the same
      *>        answer as F3, because it is the same rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1276VM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F2 ASSIGN TO "pb1276vm2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT F3 ASSIGN TO "pb1276vm3.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT F4 ASSIGN TO "pb1276vm4.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F2 RECORD IS VARYING IN SIZE DEPENDING ON L.
       01  R2.
           05 A2 PIC X(3).
           05 D2 PIC X DYNAMIC LENGTH LIMIT 20.
       FD  F3 RECORD IS VARYING IN SIZE.
       01  R3.
           05 A3 PIC X(3).
           05 D3 PIC X DYNAMIC LENGTH LIMIT 20.
       FD  F4.
       01  R4.
           05 A4 PIC X(3).
           05 D4 PIC X DYNAMIC LENGTH LIMIT 20.
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  L PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F2
           MOVE "AAA" TO A2
           MOVE "12345678901234" TO D2
           MOVE 17 TO L
           WRITE R2
           DISPLAY "F2-17=" FS
           MOVE 23 TO L
           WRITE R2
           DISPLAY "F2-23=" FS
           MOVE 24 TO L
           WRITE R2
           DISPLAY "F2-24=" FS
           CLOSE F2
           OPEN OUTPUT F3
           MOVE "AAA" TO A3
           MOVE "12345678901234" TO D3
           WRITE R3
           DISPLAY "F3=" FS
           CLOSE F3
           OPEN OUTPUT F4
           MOVE "AAA" TO A4
           MOVE "12345678901234" TO D4
           WRITE R4
           DISPLAY "F4=" FS
           CLOSE F4
           STOP RUN.
