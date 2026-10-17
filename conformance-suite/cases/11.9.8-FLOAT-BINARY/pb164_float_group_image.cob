      *>--- conformance-case
      *> id: pb164_float_group_image
      *> rules: [SR-11.9.8.3-1, GR-14.9.11.4-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB164FG.
      *> kb/Work PB164 wave 2 - the FLOAT byte-form pin (Ieee32/Ieee64,
      *> big-endian IEEE 754 interchange, 13.18.60.4 GR13-GR15; A.1
      *> item 207): a COMP-1/COMP-2-leaf group is image-capable. F -
      *> the group round-trips through an alphanumeric intermediary
      *> (14.9.25.4 GR4 - forces AsImage AND FromImage; the values
      *> come back BIT-EXACT because the lanes reinterpret, never
      *> convert). A - CALL BY REFERENCE write-through (14.2.3 GR8).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GF.
          05 F1 USAGE COMP-1 VALUE 1.5.
          05 F2 USAGE COMP-2 VALUE -2.25.
          05 FX PIC X(2) VALUE "ok".
       01 GF2.
          05 F1-2 USAGE COMP-1.
          05 F2-2 USAGE COMP-2.
          05 FX-2 PIC X(2).
       01 XF PIC X(14).
       PROCEDURE DIVISION.
       MAIN.
           MOVE GF TO XF
           MOVE XF TO GF2
           DISPLAY "F=[" F1-2 " " F2-2 " " FX-2 "]"
           CALL "SUBFG" USING GF
           DISPLAY "A=[" F1 "]"
           STOP RUN.
       END PROGRAM PB164FG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBFG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LGF.
          05 LF1 USAGE COMP-1.
          05 LF2 USAGE COMP-2.
          05 LFX PIC X(2).
       PROCEDURE DIVISION USING LGF.
       MAIN.
           ADD 1 TO LF1
           GOBACK.
       END PROGRAM SUBFG.
