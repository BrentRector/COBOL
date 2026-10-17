*>--- conformance-case
*> id: pb966-initialize-level66-alias
*> rules: [SR-14.9.20.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB966 - INITIALIZE REFUSES A LEVEL-66 IDENTIFIER-1 OF EITHER RENAMES FORM.
      *> 14.9.20.3 SR5: "The data description entry for the data item referenced by identifier-1 shall
      *> not contain a RENAMES clause." GALIAS renames the group SG WITHOUT the THROUGH phrase; by
      *> 13.18.45.4 GR1 it shares SG's attributes and storage, but its own data description entry is the
      *> one containing the RENAMES clause. The screen asked SG's entry, and this program compiled and
      *> initialized SG; the THROUGH form was already refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB966NIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 SG.
             10 S1 PIC X(3) VALUE "ABC".
          05 S2 PIC X(3) VALUE "DEF".
       66 GALIAS RENAMES SG.
       PROCEDURE DIVISION.
           INITIALIZE GALIAS
           DISPLAY SRC
           STOP RUN.
