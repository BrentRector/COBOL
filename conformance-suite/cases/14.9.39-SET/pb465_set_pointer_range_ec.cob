*>--- conformance-case
*> id: pb465_set_pointer_range_ec
*> rules: [GR-14.9.39.4-19, GR-14.9.39.4-20, DOC-A.1-216]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB465 - SET pointer UP/DOWN BY states TWO rules over one statement, and they are two rules
      *> with two exception conditions. 14.9.39.4 GR19 tests the AMOUNT's value: "If arithmetic-expression-3
      *> does not evaluate to an integer, the EC-SIZE-ADDRESS exception condition is set to exist, the
      *> execution of the SET statement is unsuccessful, and the content of identifier-9 is unchanged."
      *> GR20 tests the RESULTING ADDRESS: "... If this new address is outside the range of values allowed by
      *> the implementor for a data-pointer data item, the EC-RANGE-PTR exception condition is set to exist and
      *> the value of the data item referenced by identifier-9 is unchanged."
      *>
      *> SET pointer UP/DOWN BY is a COBOL-2002 introduction (ConstructRegistry pointer-arithmetic-2002), and
      *> so is the exception-condition mechanism it needs (>>TURN, 7.3.25), so 2002 is where the rule's own
      *> behaviour is pinned. The text of GR19 and GR20 is unchanged across 2002, 2014 and 2023.
      *>
      *> The implementor range GR20 names is docs/CONFORMANCE.md 7 DOC-A.1-216: the signed 64-bit interval of
      *> character-position displacements. 1.0E19 exceeds it and IS an integer, which is exactly the case the
      *> two rules had been conflated over - it used to set EC-SIZE-ADDRESS and abort the run unit.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>
      *> A=[C]       2.0 IS an integer value, so GR19's antecedent is FALSE and GR20 displaces the address by
      *>             2 character positions. BUF position 3 holds "C".
      *> SIZE=...    2.5 does not evaluate to an integer: GR19 sets EC-SIZE-ADDRESS, Table 13 makes it Fatal,
      *>             the declarative runs and RESUME AT NEXT STATEMENT (14.9.33) continues. FUNCTION
      *>             EXCEPTION-STATUS returns the name in a 31-character alphanumeric field, space-padded.
      *> B=[C]       GR19's other two consequents: unsuccessful, identifier-9 unchanged - still position 3.
      *> RANGE=...   1.0E19 IS an integer, so GR19 does not apply; the address it would produce is outside the
      *>             implementor range, so GR20 sets EC-RANGE-PTR instead. Same 31-character field.
      *> C=[C]       GR20's other consequent: "the value of the data item referenced by identifier-9 is
      *>             unchanged" - still position 3.
      *> D=[A]       DOWN BY 2 decrements by 2 (GR20), back to position 1.
       >>TURN EC-SIZE-ADDRESS CHECKING ON
       >>TURN EC-RANGE-PTR CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB465SETPTR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF PIC X(8) VALUE "ABCDEFGH".
       01 P USAGE POINTER.
       01 TWO-EXACT PIC 9V9 VALUE 2.0.
       01 TWO-HALF  PIC 9V9 VALUE 2.5.
       01 HUGE-AMT  USAGE FLOAT-LONG VALUE 1.0E19.
       LINKAGE SECTION.
       01 W PIC X(1) BASED.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HSIZE SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-ADDRESS.
       HSIZE-P.
           DISPLAY "SIZE=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       HRANGE SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-PTR.
       HRANGE-P.
           DISPLAY "RANGE=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET P TO ADDRESS OF BUF.
           SET P UP BY TWO-EXACT.
           SET ADDRESS OF W TO P.
           DISPLAY "A=[" W "]".
           SET P UP BY TWO-HALF.
           SET ADDRESS OF W TO P.
           DISPLAY "B=[" W "]".
           SET P UP BY HUGE-AMT.
           SET ADDRESS OF W TO P.
           DISPLAY "C=[" W "]".
           SET P DOWN BY 2.
           SET ADDRESS OF W TO P.
           DISPLAY "D=[" W "]".
           STOP RUN.
