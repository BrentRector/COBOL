      *> kb/Work PB665 + PB816 -- the CONFORMING spellings of the two
      *> restricted-data-pointer rules their negatives refuse.
      *> 14.9.39.3 SR19, last sentence: "If identifier-6 references a
      *> restricted data-pointer, either identifier-5 shall reference a
      *> data-pointer restricted to the same type or data-name-1 shall
      *> be a typed item of the type to which identifier-6 is
      *> restricted."  SET ADDRESS OF has no identifier-5, so the based
      *> receiver R2, declared TYPE T-REC, is that typed item, and the
      *> storage is read through the type it was written with.
      *> SR24: "Identifier-9 shall not be a data-pointer restricted to a
      *> type described with the STRONG phrase" -- so pointer arithmetic
      *> is legal over an UNRESTRICTED pointer and over a pointer
      *> restricted to a type described WITHOUT it (W-REC below).
      *> 8.4.3.11.4 GR2 makes ADDRESS OF a strongly-typed group item a
      *> restricted data-pointer; the address of a leaf of a WEAKLY typed
      *> record is unrestricted (8.4.3.11.3 SR2 bars only a leaf of a
      *> STRONGLY typed group).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB665OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 T-A PIC X(4).
          05 T-B PIC X(4).
       01 T-RP IS TYPEDEF USAGE POINTER TO T-REC.
       01 W-REC IS TYPEDEF.
          05 W-A PIC X(4).
          05 W-B PIC X(4).
       01 W-RP IS TYPEDEF USAGE POINTER TO W-REC.
       01 HOLDER TYPE T-REC.
       01 R2 TYPE T-REC BASED.
       01 PS TYPE T-RP.
       01 PQ TYPE T-RP.
       01 WBASE TYPE W-REC BASED.
       01 PW TYPE W-RP.
       01 WH TYPE W-REC.
       01 PU USAGE POINTER.
       01 UB PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO T-A OF HOLDER
           MOVE "WXYZ" TO T-B OF HOLDER
           SET PS TO ADDRESS OF HOLDER
           SET ADDRESS OF R2 TO PS
           DISPLAY "R2=" T-A OF R2 T-B OF R2
           SET PQ TO PS
           SET ADDRESS OF R2 TO PQ
           DISPLAY "COPY=" T-B OF R2
           ALLOCATE WBASE RETURNING PW
           MOVE "WEAK" TO W-A OF WBASE
           SET PW UP BY 4
           SET PW DOWN BY 4
           SET ADDRESS OF WBASE TO PW
           DISPLAY "WEAK=" W-A OF WBASE
           MOVE "LEFT" TO W-A OF WH
           MOVE "RGHT" TO W-B OF WH
           SET PU TO ADDRESS OF W-A OF WH
           SET PU UP BY 4
           SET ADDRESS OF UB TO PU
           DISPLAY "LEAF=" UB
           STOP RUN.
