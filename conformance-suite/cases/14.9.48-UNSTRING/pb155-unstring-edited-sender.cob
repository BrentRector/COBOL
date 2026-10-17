      *>--- conformance-case
      *> id: pb155-unstring-edited-sender
      *> rules: [SR-14.9.48.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.48.3 SR2: the UNSTRING sender shall be CATEGORY
      *> alphanumeric or national. An alphanumeric-edited item is
      *> category alphanumeric-edited - a distinct category - and the
      *> old screen's shape (no EditMask test) passed it (kb/Work
      *> PB155's sweep of the edited-category pattern).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB155N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC XXBXX VALUE "AB CD".
       01 R PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING XE INTO R
           STOP RUN.
