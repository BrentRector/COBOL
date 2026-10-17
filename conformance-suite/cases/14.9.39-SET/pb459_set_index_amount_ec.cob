*>--- conformance-case
*> id: pb459_set_index_amount_ec
*> rules: [GR-14.9.39.4-2, GR-14.9.39.4-3, GR-14.9.39.4-4, DOC-A.1-128]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB459 - the CHECKING-ON twin of pb459_set_index_amount_guards: the two conditions the SET
      *> amount rules name are now RAISED and reach a USE AFTER EXCEPTION CONDITION declarative.
      *>
      *> ISO 14.9.39.4 GR2 a) 1. a / GR3 name EC-BOUND-SUBSCRIPT for a non-integer amount; GR2 a) 1. b and
      *> GR4 a) name EC-RANGE-INDEX for a value or result "outside the limit specified in General rule 2 of
      *> 13.18.38, OCCURS clause". ISO 13.18.38.4 GR2 makes that limit the implementor's index range and names
      *> the three statements it governs: "An index may be modified only by a PERFORM VARYING statement, a
      *> SEARCH statement, and a SET statement." Both conditions are Fatal in Table 13, so the declarative runs
      *> and RESUME AT NEXT STATEMENT (14.9.33) keeps the run unit alive past it.
      *>
      *> Before PB459 EC-RANGE-INDEX had NO raise site anywhere in the compiler - the catalog carried its
      *> Table 13 row and a >>FLAG-02 flagger asked whether it was enabled, so it read as wired while it could
      *> never occur - and the two EC-BOUND-SUBSCRIPT arms could not fire either, because the emitter narrowed
      *> the amount with a (long) cast before anything could test it.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> SUB=        SET IX TO 2 succeeds; SET IX UP BY 1.5 violates GR3, so EC-BOUND-SUBSCRIPT is set to exist
      *>             and the declarative reports it. FUNCTION EXCEPTION-STATUS (15.32.3) returns the name in a
      *>             31-character alphanumeric field, so it is space-padded to 31.
      *> AFTER-SUB   GR3's other two consequents still hold after the declarative resumes: the SET was
      *>             unsuccessful and IX is unchanged => +0000000000000000002.
      *> RANGE=      SET IX TO 1 then UP BY 9223372036854775800 leaves IX at 9223372036854775801 (inside the
      *>             range). UP BY 100 would make it 9223372036854775901, outside it, so GR4 a) sets
      *>             EC-RANGE-INDEX and the declarative reports it.
      *> AFTER-RANGE GR4 a)'s other two consequents: unsuccessful, receiving operand unchanged =>
      *>             +9223372036854775801.
      *>
      *>   SUB=EC-BOUND-SUBSCRIPT
      *>   AFTER-SUB=+0000000000000000002
      *>   RANGE=EC-RANGE-INDEX
      *>   AFTER-RANGE=+9223372036854775801
      *> (each status line padded to 31 characters)
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       >>TURN EC-RANGE-INDEX CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB459SETIXEC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9(2) OCCURS 5 TIMES INDEXED BY IX.
       01 FRAC PIC 9V9 VALUE 1.5.
       01 N PIC S9(19) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HSUB SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       HSUB-P.
           DISPLAY "SUB=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       HRANGE SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-INDEX.
       HRANGE-P.
           DISPLAY "RANGE=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET IX TO 2.
           SET IX UP BY FRAC.
           SET N TO IX.
           DISPLAY "AFTER-SUB=" N.
           SET IX TO 1.
           SET IX UP BY 9223372036854775800.
           SET IX UP BY 100.
           SET N TO IX.
           DISPLAY "AFTER-RANGE=" N.
           STOP RUN.
