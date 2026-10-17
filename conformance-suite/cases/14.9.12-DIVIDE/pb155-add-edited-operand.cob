      *>--- conformance-case
      *> id: pb155-add-edited-operand
      *> rules: [SR-14.9.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.2.3 SR2 / 8.8.1.1: an arithmetic operand shall be a
      *> NUMERIC data item. An alphanumeric-edited item (category
      *> alphanumeric-edited, class alphanumeric - 8.5.2.1 Table 2) is
      *> not one; the old screen's `EditMask: null` pattern let it slip
      *> and digit-decode under STRICT (kb/Work PB155).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC XXBXX VALUE "AB CD".
       01 N PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD XE TO N
           STOP RUN.
