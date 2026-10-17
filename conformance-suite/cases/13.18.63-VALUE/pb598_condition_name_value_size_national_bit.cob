      *>--- conformance-case
      *> id: pb598_condition_name_value_size_national_bit
      *> rules: [SR-13.18.63.3-5, SR-13.18.63.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB598P2.
      *> ISO 13.18.63.3 SR5 (national) and SR10 (boolean) - each the
      *> SAME sentence pair as SR4's, over national and over boolean
      *> positions: "National literals in the VALUE clause of AN
      *> ELEMENTARY ITEM shall not exceed the size indicated by AN
      *> EXPLICIT PICTURE clause.  National literals in the VALUE clause
      *> of a national group item shall not exceed the size of the group
      *> item."  SR10 is written for boolean and reaches the
      *> condition-name format through SR24, "Syntax rules 10 and 17
      *> above apply".
      *>
      *> THE OTHER TWO ARMS of kb/Work PB598.  SR5 is an ALL FORMATS
      *> rule and SR10 is imported WHOLE by SR24, so both bind a
      *> Format-3 entry - but their SIZE sentences name only an
      *> elementary item and a group item, and a condition-name is
      *> neither (13.16.2's Format 3 admits no PICTURE clause; SR33
      *> makes the level-88 entry the subject; 8.5.1.3.2 item 3 gives it
      *> no concept of level; 13.18.63.4 GR19 gives it its conditional
      *> variable's characteristics only IMPLICITLY, which is what
      *> "explicit" excludes).  Measured before this fix: both were
      *> COBOLNET0898, legal source rejected - and unlike the
      *> alphanumeric arm these two had been wrong since they were
      *> written, which is why no differential case caught them (0 of
      *> 1,611 GnuCOBOL corpus members carries an oversize national or
      *> boolean level-88 literal).
      *>
      *> The CLASS half of the same rules is untouched and still binds a
      *> condition-name: N1/B1 below take only their own literal forms,
      *> which negative fixtures pin.
      *>
      *> Derived expectations:
      *> N1  NV = N"AB"; NC is N"ABC"; 8.8.4.5.3 item 2 -> relation
      *>     rules -> 8.8.4.2.9 (national operands, standard comparison
      *>     absent a locale-based national collating sequence) ->
      *>     8.8.4.2.10 item 2, "the shorter operand were extended on
      *>     the right by sufficient national spaces", so N"AB " differs
      *>     from N"ABC" at position 3 -> FALSE.
      *> N2  SET NC TO TRUE -> 14.9.39.4 GR6 -> 13.18.63.4 GR7 ->
      *>     14.6.8.5 truncation to the right -> NV = N"AB", and NC is
      *>     still FALSE.
      *> B1  BV = B"10"; BC is B"101"; 8.8.4.2.8 item 2 extends the
      *>     shorter operand "on the right by sufficient boolean zeros"
      *>     - NOT spaces; there is no boolean space - so B"100" differs
      *>     from B"101" at position 3 -> FALSE.  This is the arm a
      *>     screen that borrowed the alphanumeric rule would get wrong.
      *> B2  SET BC TO TRUE truncates B"101" into two boolean positions
      *>     -> BV = B"10" (displayed "10"), and BC is still FALSE.
      *> B3  The BOUNDARY: DC is B"10" over two positions -> TRUE.
      *>
      *> National data and boolean data are COBOL-2002 introductions
      *> (COBOLNET0900 below that), which is why this half of PB598's
      *> proof lives at 2002 and not at 85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NV PIC N(2).
          88 NC VALUE N"ABC".
       01 BV PIC 1(2) USAGE BIT.
          88 BC VALUE B"101".
       01 DV PIC 1(2) USAGE BIT.
          88 DC VALUE B"10".
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"AB" TO NV
           IF NC DISPLAY "N1=TRUE" ELSE DISPLAY "N1=FALSE" END-IF
           SET NC TO TRUE
           DISPLAY "N2=[" NV "]"
           IF NC DISPLAY "N2B=TRUE" ELSE DISPLAY "N2B=FALSE" END-IF
           MOVE B"10" TO BV
           IF BC DISPLAY "B1=TRUE" ELSE DISPLAY "B1=FALSE" END-IF
           SET BC TO TRUE
           DISPLAY "B2=[" BV "]"
           IF BC DISPLAY "B2B=TRUE" ELSE DISPLAY "B2B=FALSE" END-IF
           MOVE B"10" TO DV
           IF DC DISPLAY "B3=TRUE" ELSE DISPLAY "B3=FALSE" END-IF
           STOP RUN.
