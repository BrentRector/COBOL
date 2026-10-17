      *>--- conformance-case
      *> id: pb155-divide-into-alpha
      *> rules: [SR-14.9.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.12.3 SR1: DIVIDE's in-place INTO receiver shall be an
      *> elementary data item of category NUMERIC. The ONE receiving
      *> chokepoint (ScreenResultant, kb/Work PB128) enforces it; this
      *> pins the DIVIDE-shaped call site (editedOk:false citing SR1) -
      *> the batch-8 adjudication read ResolveReceiving alone and
      *> recorded the half as unenforced (kb/Work PB155).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ALPHA-ITEM PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE 2 INTO ALPHA-ITEM
           STOP RUN.
