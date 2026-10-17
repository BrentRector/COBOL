      *>--- conformance-case
      *> id: pb393_corresponding_odo_group
      *> rules: [SR-14.9.44.3-6, GR-14.9.44.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB393. ISO 1989:2023 14.9.44.3 SR6: "Identifier-4 and identifier-5 shall be alphanumeric group
      *> items, national group items, variable-length groups, or strongly-typed group items and shall not be
      *> described with level-number 66." An occurs-depending group is an alphanumeric group item, so all three
      *> CORRESPONDING verbs admit it. 14.7.6 rule 4 then EXCLUDES the OCCURS item itself from correspondence,
      *> leaving exactly the scalar pair P - so the implied statements are SUBTRACT P OF SRC FROM P OF DST
      *> (14.9.44.4 GR5), ADD, and MOVE.
      *>   S= 10 - 3 = 0007 (SUBTRACT CORRESPONDING).   A= 7 + 3 = 0010 (ADD CORRESPONDING).
      *>   M= 0003 (MOVE CORRESPONDING copies P).
      *> Q is the occurs-depending member and is left untouched by all three - Q= shows the value stored before
      *> the statements, which rule 4's exclusion is what preserves.
      *> Before this fixture the operand resolved to an OdoGroupPlace and CorrAccess.Create's storage-form
      *> switch met it in `_ => null`: every one of these statements compiled clean and ABORTED the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393CORRODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(3) VALUE 2.
       01 SRC.
          05 P PIC 9(3) VALUE 3.
          05 Q PIC X(2) OCCURS 1 TO 5 TIMES DEPENDING ON CNT.
       01 DST.
          05 P PIC 9(4) VALUE 10.
          05 Q PIC X(2) OCCURS 1 TO 5 TIMES DEPENDING ON CNT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "QQ" TO Q OF DST (1).
           SUBTRACT CORRESPONDING SRC FROM DST.
           DISPLAY "S=" P OF DST.
           ADD CORRESPONDING SRC TO DST.
           DISPLAY "A=" P OF DST.
           MOVE CORRESPONDING SRC TO DST.
           DISPLAY "M=" P OF DST.
           DISPLAY "Q=[" Q OF DST (1) "]".
           STOP RUN.
