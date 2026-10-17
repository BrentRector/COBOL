      *>--- conformance-case
      *> id: pb966_move_corresponding_through_alias
      *> rules: [GR-14.7.6-1, SR-14.9.25.3-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB966 - MOVE CORRESPONDING THROUGH A LEVEL-66 ALIAS.
      *>
      *> 14.9.25.3 SR12: "Identifier-3 and identifier-4 shall specify group data items and shall not be
      *> reference-modified." 13.18.45.4 GR2: "When the THROUGH phrase is specified, data-name-1 defines
      *> an alphanumeric group item that includes all elementary items starting with data-name-2 ... or
      *> the first elementary item in data-name-2 (if data-name-2 is a group item), and concluding with
      *> data-name-3". So a THROUGH alias IS a group data item and SR12 admits it on either side; this
      *> program used to be refused COBOLNET1757 "described with level-number 66, so it is not a group
      *> data item". SR12 names no level number (ADD/SUBTRACT SR6 do; they still refuse it).
      *>
      *> DETERMINATION (docs/CONFORMANCE.md section 7): 14.7.6 rule 1 pairs items with "the same data-name
      *> and the same qualifiers, if any, up to, but not including, D1 and D2". An alias includes the
      *> ELEMENTARY items GR2 names and no groups, so each included item stands directly in the alias
      *> with no qualifier between: it corresponds by name with a first-level item of the other operand.
      *> Rules 4 and 5 still exclude an included item under a REDEFINES or OCCURS entry inside the window;
      *> rule 6 excludes a name the alias includes twice.
      *>
      *> EXPECTED VALUES, DERIVED (DST is reset to all "*" before each MOVE):
      *>   L1 - MOVE CORRESPONDING SALIAS (SG THROUGH S3) TO DST. Included: S1 N1 S2 DUP DUP S5 SR S3.
      *>        S1 "ABC"; N1 123 into 9(5) = "00123" (14.9.25.4 numeric move); S2 "DEF"; DUP is
      *>        included twice - rule 6, no pair, "*" kept; S5 lies under SRR REDEFINES SR inside the
      *>        window - rule 5, "**" kept; SR "RR"; S3 "GHI". S0 is outside the window ("**" kept) and
      *>        DST's SG is not matched because GR2 includes no group ("******" kept).
      *>   L2 - MOVE CORRESPONDING R2 TO TALIAS (T1 THRU T2): the alias is the RECEIVER. Its members are
      *>        T1, S2, S3 (T2's elementary items, unqualified). R2's S2 and S3 correspond; T1 has no
      *>        namesake and keeps "aaa". TGT = "aaa222333".
      *>   L3 - MOVE CORRESPONDING GALIAS TO DST, GALIAS RENAMES SG without THROUGH: 13.18.45.4 GR1
      *>        gives it SG's attributes, so it is SG's group and its S1 and N1 correspond.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB966MCA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 S0 PIC X(2) VALUE "ZZ".
          05 SG.
             10 S1 PIC X(3) VALUE "ABC".
             10 N1 PIC 9(3) VALUE 123.
          05 S2 PIC X(3) VALUE "DEF".
          05 SH.
             10 DUP PIC X VALUE "1".
          05 SI.
             10 DUP PIC X VALUE "2".
          05 SR PIC X(2) VALUE "RR".
          05 SRR REDEFINES SR.
             10 S5 PIC X(2).
          05 S3 PIC X(3) VALUE "GHI".
       66 SALIAS RENAMES SG THROUGH S3.
       66 GALIAS RENAMES SG.
       01 DST.
          05 S0 PIC X(2).
          05 S1 PIC X(3).
          05 N1 PIC 9(5).
          05 S2 PIC X(3).
          05 DUP PIC X.
          05 S5 PIC X(2).
          05 SR PIC X(2).
          05 S3 PIC X(3).
          05 SG PIC X(6).
       01 R2.
          05 S1 PIC X(3) VALUE "111".
          05 S2 PIC X(3) VALUE "222".
          05 S3 PIC X(3) VALUE "333".
       01 TGT.
          05 T1 PIC X(3) VALUE "aaa".
          05 T2.
             10 S2 PIC X(3) VALUE "bbb".
             10 S3 PIC X(3) VALUE "ccc".
       66 TALIAS RENAMES T1 THRU T2.
       PROCEDURE DIVISION.
           MOVE ALL "*" TO DST
           MOVE CORRESPONDING SALIAS TO DST
           DISPLAY "L1 [" DST "]"
           MOVE CORRESPONDING R2 TO TALIAS
           DISPLAY "L2 [" TGT "]"
           MOVE ALL "*" TO DST
           MOVE CORRESPONDING GALIAS TO DST
           DISPLAY "L3 [" DST "]"
           STOP RUN.
