      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB643 - RESERVE integer-1 above this implementation's
      *> limit of 524,287 input-output areas is refused (COBOLNET2897).
      *>   cite.py --check 12.4.5.14.3 "If the RESERVE clause is
      *>     specified, the number of input-output areas allocated is
      *>     equal to the value of integer-1" -> OK §12.4.5.14.3 1)
      *>   cite.py --check 4.2.15 "A conforming implementation may
      *>     place such limits" -> OK §4.2.15
      *> A connector's areas are ONE buffer of 4,096 bytes per area
      *> whose length is a 32-bit count, so 524,288 areas cannot be
      *> allocated, and GR1 makes the count exact: refused, never
      *> silently reduced (docs/CONFORMANCE.md DOC-A.1-164).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB643LIM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb643lim.dat"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 524288 AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       PROCEDURE DIVISION.
           OPEN OUTPUT F.
           CLOSE F.
           STOP RUN.
