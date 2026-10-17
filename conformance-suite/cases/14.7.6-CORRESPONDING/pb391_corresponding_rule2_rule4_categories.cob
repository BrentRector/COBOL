*>--- conformance-case
*> id: pb391_corresponding_rule2_rule4_categories
*> rules: [GR-14.7.6-4, SR-14.9.25.3-13, GR-14.9.25.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB391. The 2002-and-later halves of ISO 1989:2023 14.7.6's CORRESPONDING rules, which the
      *> COBOL-85 fixture (85/pb391_corresponding_rule2_table16_85) cannot express.
      *>
      *> RULE 2 - "the resulting move is valid according to the rules for the MOVE statement" - over the
      *> categories 14.9.25.3 table 16 adds beyond the '85 set, read AS PRINTED:
      *>   National           -> National, National-edited        = Yes  NA2 becomes XYZ
      *>   Boolean            -> Boolean                          = Yes  BO2 becomes 1010
      *>   Numeric/Integer    -> Boolean                          = No   NB2 keeps 0000
      *>   Numeric/Noninteger -> National, National-edited        = No   NV2 keeps QQQ
      *> A refused pair is a SILENT non-selection, so the receiver keeps its prior content; MM proves the
      *> statement ran. 14.9.25.4 GR11 NOTE 5 is why the elementary national and boolean children are reached
      *> at all: "bit group items and national group items are processed as group items", so the binder
      *> descends into them and pairs their elementary members.
      *>
      *> RULE 4 - "Neither data item contains an OCCURS, REDEFINES, or RENAMES clause or is of class index,
      *> message-tag, object, or pointer." K is class pointer in both groups, so the namesake pair is NOT a
      *> corresponding pair and PK stays NULL while the alphanumeric sibling MP moves. Table 16 has no pointer
      *> row or column and therefore ADMITS a pointer x pointer pair - rule 4 is the only thing refusing it,
      *> which is exactly what CorrespondingRule2DriftTests pins. 13.18.60.3 SR14 is why the pointer sits
      *> under a STRONG type declaration: that is the one spelling legal below level 1.
      *>
      *> Before PB391 the binder answered rule 2 from a second, private, partial copy of table 16 whose sender
      *> switch ended `_ => false` and whose receiver axis was one boolean: NA2 and BO2 were silently DROPPED,
      *> NB2 read 1010 and NV2 read 035 - and the pointer pair was excluded only by that same `_ => false`
      *> accident, so deleting the copy without rule 4 would have copied a pointer silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB391CORRCAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PTRREC IS TYPEDEF STRONG.
          05 K USAGE POINTER.
          05 MP PIC X(3).
       01 G1.
          05 NA PIC N(3) VALUE N"XYZ".
          05 BO PIC 1(4) USAGE BIT VALUE B"1010".
          05 NB PIC 9(4) VALUE 1010.
          05 NV PIC 9(2)V9 VALUE 3.5.
          05 MM PIC X(3) VALUE "AAA".
       01 G2.
          05 NA PIC N(3) VALUE N"QQQ".
          05 BO PIC 1(4) USAGE BIT.
          05 NB PIC 1(4) USAGE BIT.
          05 NV PIC N(3) VALUE N"QQQ".
          05 MM PIC X(3) VALUE "BBB".
       01 P1 TYPE PTRREC.
       01 P2 TYPE PTRREC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AAA" TO MP OF P1.
           MOVE "BBB" TO MP OF P2.
           MOVE CORRESPONDING G1 TO G2.
           MOVE CORRESPONDING P1 TO P2.
           DISPLAY "NA2=[" NA OF G2 "]".
           DISPLAY "BO2=[" BO OF G2 "]".
           DISPLAY "NB2=[" NB OF G2 "]".
           DISPLAY "NV2=[" NV OF G2 "]".
           DISPLAY "MM2=[" MM OF G2 "]".
           DISPLAY "MP2=[" MP OF P2 "]".
           IF K OF P2 = NULL
              DISPLAY "PK2=[NULL]"
           ELSE
              DISPLAY "PK2=[SET]"
           END-IF.
           STOP RUN.
