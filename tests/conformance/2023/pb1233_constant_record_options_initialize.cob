      *> ISO/IEC 1989:2023 13.18.15.4 GR1 + 14.9.20.4 GR5/GR6 (kb/Work PB1233), under an OPTIONS INITIALIZE fill.
      *> OPTIONS INITIALIZE (11.9.10.4 GR5) lays a background over the initial state of working-storage, and
      *> 11.9.10.4 GR7's exception admits a CONSTANT RECORD to that initial state - but its CONTENT is that of an
      *> INITIALIZE (13.18.15.4 GR1), and an INITIALIZE sends each VALUE-less leaf its category's figurative
      *> constant (14.9.20.4 GR6 c): the fill must not reach it. An ordinary record in the same program is
      *> filled, so the fill is genuinely in force. Every storage lane of a record seeds the same content: the
      *> record-struct field (CR), a REDEFINES window over it (RV), a second record (CP) and an EXTERNAL one
      *> (CT, the run-unit cell). (This golden used to take ADDRESS OF CP, to reach the shared-cell lane; 8.4.3.11.3
      *> SR3 -- "Identifier-1 shall not reference a data item that is described with the CONSTANT RECORD clause" --
      *> forbids that operand, so a constant record never lives on an address-taken cell: kb/Work PB1407.)
      *> WHY EACH LINE CAN FAIL:
      *>   CR=   [     0.00  0  0]  A1 is SPACES (not Z), E1 and both K occurrences the edited zero (not Z).
      *>   RV=   [  0.00]          the REDEFINES window reads the same edited zero.
      *>   CP=   [    0]           the second record: PA spaces, PE the edited zero.
      *>   P=    [ZZZZ]            the ordinary item IS filled with X"5A".
      *>   EXT=  [  0]             the EXTERNAL constant's shared cell seeds the edited zero (not Z).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1233CROPT.
       OPTIONS.
           INITIALIZE WORKING-STORAGE SECTION TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR CONSTANT RECORD.
           05 A1 PIC X(3).
           05 E1 PIC ZZ9.99.
           05 RV REDEFINES E1 PIC X(6).
           05 K PIC ZZ9 OCCURS 2.
       01 CP CONSTANT RECORD.
           05 PA PIC X(2).
           05 PE PIC ZZ9.
       01 P PIC X(4).
       01 CTT TYPEDEF STRONG IS EXTERNAL.
           05 CT-E PIC ZZ9.
       01 CT IS EXTERNAL CONSTANT RECORD TYPE CTT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "CR=[" CR "]"
           DISPLAY "RV=[" RV "]"
           DISPLAY "CP=[" CP "]"
           DISPLAY "P=[" P "]"
           DISPLAY "EXT=[" CT-E IN CT "]"
           STOP RUN.
