      *> kb/Work PB2518 - ISO/IEC 1989:2023 section 13.18.44.3 SR10: "The entries giving the new descriptions of the
      *> storage area shall follow the entries defining the area of data-name-2, without intervening entries that
      *> define new storage areas."
      *>   cite.py --check 13.18.44.3 "without intervening entries that define new storage areas" -> OK  10)
      *> A CONSTANT entry (13.10) describes no data item and a TYPEDEF entry (13.18.58) allocates no storage - it is a
      *> type declaration - so neither is an entry that defines a new storage area, and a level-01 REDEFINES may follow
      *> data-name-2 across them. The 85 golden pb2518_redefines_only_redefinitions_between covers the plain forms;
      *> the negative twins are conformance/negative/pb2518-redefines-past-intervening-storage and ...-roots.
      *>
      *> DERIVATION. V is two characters "ab"; the constant K and the type T (a one-character field) come between it
      *> and its redefinition VR (13.18.44.4 GR1: the same storage from the first character of V).
      *> L1: VR reads V: ab. L2: MOVE "yz" TO VR is seen through V: yz. L3: an item TYPEd as T is its own storage,
      *> one character: Q.
      *> L4: a LEGAL redefinition inside a type declaration (TB over TA, the declaration walked since the same change
      *> screens redefiners inside a TYPEDEF) still binds in its clone: TA is "mn" (13.18.57.4 GR1: the TYPE clause is
      *> "as though the data description identified by type-name-1 had been coded in" the entry, cite.py --check OK),
      *> TB is its first character: m.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2518TD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC XX VALUE "ab".
       01 K CONSTANT AS 5.
       01 T TYPEDEF.
          05 TF PIC X.
       01 VR REDEFINES V PIC XX.
       01 TQ TYPE T.
       01 T2 TYPEDEF.
          05 TA PIC XX VALUE "mn".
          05 TB REDEFINES TA PIC X.
       01 TQ2 TYPE T2.
       PROCEDURE DIVISION.
           DISPLAY "L1=" VR
           MOVE "yz" TO VR
           DISPLAY "L2=" V
           MOVE "Q" TO TF OF TQ
           DISPLAY "L3=" TF OF TQ
           DISPLAY "L4=" TB OF TQ2
           STOP RUN.
