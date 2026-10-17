*>--- conformance-case
*> id: pb459_set_index_range_ec
*> rules: [GR-13.18.38.4-2, GR-14.9.39.4-2, GR-14.9.39.4-4, DOC-A.1-128]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB459 - the EDITION leg: EC-RANGE-INDEX and the SET amount guards are not a 2023 feature.
      *>
      *> ISO 13.18.38.4 GR2 and 14.9.39.4 GR2 a) 1. / GR3 / GR4 a) are unchanged text across 2002, 2014 and
      *> 2023; the exception-condition mechanism itself (and with it >>TURN, 7.3.25) is what arrives in 2002.
      *> So a 2002 program that turns EC-RANGE-INDEX checking on must see exactly what its 2023 twin
      *> (2023/pb459_set_index_amount_ec.cob) sees - that is the whole point of listing it here rather than
      *> assuming the behaviour travels. At --std 85 there is no >>TURN and no exception condition, so the
      *> lenient half is what remains; 85/pb459_set_index_overflow_unchanged.cob pins that leg.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> RANGE=      SET IX TO 1 then UP BY 9223372036854775800 leaves IX at 9223372036854775801, inside the
      *>             implementor index range (13.18.38.4 GR2; docs/CONFORMANCE.md 7 DOC-A.1-128 documents it as
      *>             the signed 64-bit interval). UP BY 100 would make it 9223372036854775901, outside it, so
      *>             GR4 a) sets EC-RANGE-INDEX; Table 13 makes it Fatal, the declarative runs and RESUME AT
      *>             NEXT STATEMENT (14.9.33) continues. FUNCTION EXCEPTION-STATUS returns the name in a
      *>             31-character alphanumeric field, space-padded.
      *> AFTER=      GR4 a)'s other two consequents: "the execution of the SET statement is unsuccessful, and
      *>             the content of the receiving operand is unchanged" => +9223372036854775801.
      *>
      *>   RANGE=EC-RANGE-INDEX
      *>   AFTER=+9223372036854775801
      *> (the status line padded to 31 characters)
       >>TURN EC-RANGE-INDEX CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB459SETIX02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
       01 N PIC S9(19) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HRANGE SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-INDEX.
       HRANGE-P.
           DISPLAY "RANGE=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET IX TO 1.
           SET IX UP BY 9223372036854775800.
           SET IX UP BY 100.
           SET N TO IX.
           DISPLAY "AFTER=" N.
           STOP RUN.
