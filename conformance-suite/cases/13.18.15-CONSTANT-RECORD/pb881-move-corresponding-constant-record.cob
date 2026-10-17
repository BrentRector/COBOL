*>--- conformance-case
*> id: pb881-move-corresponding-constant-record
*> rules: [SR-13.18.15.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB881 — ISO 13.18.15.3 SR2 over MOVE CORRESPONDING: identifier-2 is the receiving group, and every
*> subordinate item paired with it is stored into, so a CONSTANT RECORD shall not be it. The CORRESPONDING
*> binder resolved the receiving group with the plain reference resolver and rewrote the constant's members.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB881NMC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S.
          05 F1 PIC X(3) VALUE "new".
       01 CG CONSTANT RECORD.
          05 F1 PIC X(3) VALUE "old".
       PROCEDURE DIVISION.
       MAIN.
           MOVE CORRESPONDING S TO CG
           STOP RUN.
