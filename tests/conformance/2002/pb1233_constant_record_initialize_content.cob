      *> ISO/IEC 1989:2023 13.18.15.4 GR1 (kb/Work PB1233): the content of a record described with the CONSTANT
      *> RECORD clause is as though the clause had been omitted and the record had been the subject of
      *> INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT. By 14.9.20.4 GR5 c) 1. b a leaf takes its VALUE
      *> only when its OWN entry carries a data-item VALUE clause (a group-level VALUE is no sender, GR6), and by
      *> GR6 c) every other leaf receives its category's figurative constant by MOVE - ZEROES for a numeric-edited
      *> item, SPACES for an alphanumeric one, FILLER included (the FILLER phrase).
      *> WHY EACH LINE CAN FAIL (an initial-state seed leaves the numeric-edited items blank):
      *>   E1=   [  0.00]  ZZ9.99 <- ZEROES, edited; the ordinary initial state of a VALUE-less item is spaces.
      *>   C1=   [000]     a binary item <- ZEROES.
      *>   V1=   [OK]      its own VALUE clause applies.
      *>   GV=   [    ]    GA and GB have no VALUE of their own: the group VALUE "WXYZ" is no INITIALIZE sender.
      *>   T=    [  0  0]  every occurrence of a table element is a receiving operand, ZZ9 <- ZEROES.
      *>   EV=   [  7]     a VALUE-bearing numeric-edited leaf keeps its own VALUE (the edited form as written,
      *>                   13.18.63.3 SR7).
      *>   CRG=  the whole record as one alphanumeric group: 5 spaces (A1, FILLER), the edited zero, "OK".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1233CRPOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR CONSTANT RECORD.
           05 A1 PIC X(3).
           05 FILLER PIC X(2).
           05 E1 PIC ZZ9.99.
           05 C1 PIC 9(3) COMP.
           05 V1 PIC X(2) VALUE "OK".
           05 GV VALUE "WXYZ".
               10 GA PIC XX.
               10 GB PIC XX.
           05 T PIC ZZ9 OCCURS 2.
           05 EV PIC ZZ9 VALUE "  7".
       01 CRG CONSTANT RECORD.
           05 G-A1 PIC X(3).
           05 FILLER PIC X(2).
           05 G-E1 PIC ZZ9.99.
           05 G-V1 PIC X(2) VALUE "OK".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "E1=[" E1 "]"
           DISPLAY "C1=[" C1 "]"
           DISPLAY "V1=[" V1 "]"
           DISPLAY "GV=[" GV "]"
           DISPLAY "T=[" T(1) T(2) "]"
           DISPLAY "EV=[" EV "]"
           DISPLAY "CRG=[" CRG "]"
           STOP RUN.
