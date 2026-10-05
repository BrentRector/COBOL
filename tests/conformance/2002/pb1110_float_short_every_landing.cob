      *> PB1110 - ISO 14.6.8.3 rule 1: the content of a FLOAT-SHORT
      *>   receiving operand "is set to the algebraic value of the
      *>   sending operand"; WiseOwl COBOL's determination (CONFORMANCE
      *>   DOC-A.1-81) is ONE correctly rounded conversion into the
      *>   receiver's own binary32 format, whatever statement lands it.
      *> cite.py --check 14.6.8.3 "the content of the receiving
      *>   operand is set to the algebraic value of the sending
      *>   operand" -> OK  14.6.8.3 1)
      *> Derivation: the binary32 neighbours of 1 are 1 and 1 + 2**-23 =
      *>   1.00000011920928955078125; their midpoint is 1 + 2**-24 =
      *>   1.000000059604644775390625. The algebraic value
      *>   1.0000000596046448 is ABOVE that midpoint by 2.5E-17 (less
      *>   than half a binary64 ulp, so a binary64 detour lands ON the
      *>   midpoint and ties down to 1): the single conversion is
      *>   1 + 2**-23. ONE + TINY below is the same value as a COMPUTE
      *>   or ADD ROUNDED result (nearest, so the mode cannot matter).
      *> Every landing below must store 1 + 2**-23: MOVE from a fixed,
      *>   packed, binary, edited and national sender; COMPUTE and ADD
      *>   ROUNDED; INITIALIZE REPLACING; the VALUE clause; COMP-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1110L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FS USAGE FLOAT-SHORT.
       01 C1 USAGE COMP-1.
       01 FV USAGE FLOAT-SHORT VALUE 1.0000000596046448.
       01 R PIC 9V9(23).
       01 SRC-DISP PIC 9V9(16) VALUE 1.0000000596046448.
       01 SRC-PACK PIC 9V9(16) COMP-3 VALUE 1.0000000596046448.
       01 SRC-BIN PIC 9V9(16) COMP VALUE 1.0000000596046448.
       01 SRC-EDIT PIC 9.9(16) VALUE "1.0000000596046448".
       01 SRC-NAT PIC 9V9(16) USAGE NATIONAL VALUE 1.0000000596046448.
       01 ONE PIC 9V9(16) VALUE 1.
       01 TINY PIC 9V9(16) VALUE 0.0000000596046448.
       PROCEDURE DIVISION.
           MOVE FV TO R
           DISPLAY "VALUE   " R
           MOVE SRC-DISP TO FS
           MOVE FS TO R
           DISPLAY "DISPLAY " R
           MOVE SRC-PACK TO FS
           MOVE FS TO R
           DISPLAY "PACKED  " R
           MOVE SRC-BIN TO FS
           MOVE FS TO R
           DISPLAY "BINARY  " R
           MOVE SRC-EDIT TO FS
           MOVE FS TO R
           DISPLAY "EDITED  " R
           MOVE SRC-NAT TO FS
           MOVE FS TO R
           DISPLAY "NATIONAL" R
           MOVE SRC-DISP TO C1
           MOVE C1 TO R
           DISPLAY "COMP-1  " R
           COMPUTE FS ROUNDED = SRC-DISP
           MOVE FS TO R
           DISPLAY "COMPUTE " R
           COMPUTE FS ROUNDED = ONE + TINY
           MOVE FS TO R
           DISPLAY "COMPSUM " R
           ADD TINY TO ONE GIVING FS ROUNDED
           MOVE FS TO R
           DISPLAY "ADDGIV  " R
           INITIALIZE FS REPLACING NUMERIC BY 1.0000000596046448
           MOVE FS TO R
           DISPLAY "INITREPL" R
           STOP RUN.
