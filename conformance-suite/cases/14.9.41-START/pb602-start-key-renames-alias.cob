*>--- conformance-case
*> id: pb602-start-key-renames-alias
*> rules: [SR-14.9.41.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB602 - ISO 14.9.41.3 SR5's identity again, through the OTHER reference form that shares one
      *> item's storage while being another item: a non-THROUGH level-66 alias. ISO 13.18.45.4 GR1 - "When the
      *> THROUGH phrase is not specified, all of the data attributes of data-name-2 become the data attributes of
      *> data-name-1 and the storage area occupied by data-name-2 becomes the storage area occupied by
      *> data-name-1" - shares the attributes and the storage, NOT the name: RK-ALIAS is a data item of its own,
      *> and it is not the item the RELATIVE KEY clause specifies.
      *> The THROUGH form was already refused here, because it composes a place of its own; the non-THROUGH form
      *> forwarded to the renamed item's place outright and became indistinguishable from writing WS-RK. Two
      *> spellings of one rule, one arm implemented (feedback_two_arm_dispatch).
      *> The positive control - where the same alias is a legal MOVE and ADD receiver - is
      *> tests/conformance/85/pb602_start_key_identity.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB602NEGALIAS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb602na.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-RK.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 RL-REC PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-KEYS.
          05 WS-RK  PIC 9(4).
          05 WS-PAD PIC X(2).
       66 RK-ALIAS RENAMES WS-RK.
       PROCEDURE DIVISION.
           OPEN INPUT RLF
           START RLF KEY IS = RK-ALIAS
               INVALID KEY CONTINUE
           END-START
           CLOSE RLF
           STOP RUN.
