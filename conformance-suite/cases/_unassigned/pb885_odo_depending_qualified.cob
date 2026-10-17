      *>--- conformance-case
      *> id: pb885_odo_depending_qualified
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> AN OCCURS ... DEPENDING ON OPERAND IS A QUALIFIED-DATA-NAME (kb/Work PB885, the data-division arm).
      *> ISO/IEC 1989:2023 §13.18.38.2 Format 2 prints DEPENDING ON data-name-1, and a data-name may be
      *> QUALIFIED — §8.4.2.2.1: "Identical user-defined names may be specified in a source unit; however,
      *> uniqueness shall be established through qualification for each user-defined name explicitly
      *> referenced". The capture used to be the whole reference's text, so CNT OF G1 became the undefined
      *> name CNTOFG1 and this conforming program was REJECTED (COBOLNET0851 "not defined").
      *>
      *> DERIVATION. CNT OF G1 = 2 and CNT OF G2 = 3, so only the qualifier decides which is data-name-1.
      *> §13.18.38.4 GR7: "The value of the data item referenced by data-name-1 represents the current
      *> number of occurrences of the subject of the entry." So T1 is TWO
      *> characters long: MOVE ALL "A" fills both, and DISPLAY of the group shows [AA]. Binding CNT OF G2
      *> would show [AAA].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB885ODQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 CNT PIC 9 VALUE 2.
       01 G2.
          05 CNT PIC 9 VALUE 3.
       01 T1.
          05 E1 PIC X OCCURS 1 TO 5 DEPENDING ON CNT OF G1.
       PROCEDURE DIVISION.
           MOVE ALL "A" TO T1
           DISPLAY "[" T1 "]"
           STOP RUN.
