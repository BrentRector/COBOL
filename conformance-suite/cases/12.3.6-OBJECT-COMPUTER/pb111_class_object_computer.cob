*>--- conformance-case
*> id: pb111_class_object_computer
*> rules: [GR-12.3.6.4-8, GR-12.3.6.4-11, GR-14.6.6-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 11.3 / 12.3.6: a CLASS-ID's ENVIRONMENT DIVISION — its OBJECT-COMPUTER paragraph's PROGRAM COLLATING SEQUENCE
      *> and CHARACTER CLASSIFICATION clauses — applies to the class's METHODS, each of which is a runtime element
      *> (14.6.6 r2 "On activation of a runtime element"; 12.3.6.4 GR8 / GR11). kb/Work PB111: both clauses used to be a
      *> Roslyn CS0103 on the emitted class (the program class alone declared __COLLATE / __CLASSIFY); the ONE
      *> ObjectComputerEmit helper now serves the program, instance and factory classes, and a method resolves its
      *> classification at ITS activation (an activation local — a method is re-entered on the same object).
      *>
      *> What each line proves:
      *>   CLS-UP  — a factory method of a class with CHARACTER CLASSIFICATION IS TR: UPPER-CASE("i") → U+0130 (ORD 305),
      *>             the Turkish LC_CTYPE (15.97.4 r3), witnessed by FUNCTION ORD.
      *>   CLS-REL — an object method of a class with PROGRAM COLLATING SEQUENCE IS REV ("Z" THRU "A"): "A" < "B" is FALSE
      *>             under REV (12.3.6.4 GR11 — the class's sequence governs the method's comparisons) → "GT".
      *>   PRG-REL — the invoking PROGRAM has no collating sequence: the same relation is TRUE natively → "LT".
      *> Every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB111MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB111CLS
           CLASS PB111REV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S               PIC X(4).
       01  N               PIC 9(5).
       01  O               USAGE OBJECT REFERENCE PB111REV.
       PROCEDURE DIVISION.
           INVOKE PB111CLS "UPM" RETURNING S
           MOVE FUNCTION ORD(S(1:1)) TO N
           DISPLAY "CLS-UP=" N
           INVOKE PB111REV "NEW" RETURNING O
           INVOKE O "RELM" RETURNING S
           DISPLAY "CLS-REL=" S
           IF "A" < "B" MOVE "LT" TO S ELSE MOVE "GT" TO S END-IF
           DISPLAY "PRG-REL=" S
           STOP RUN.
       END PROGRAM PB111MAIN.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB111CLS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS TR.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. UPM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  R               PIC X(4).
       PROCEDURE DIVISION RETURNING R.
       MAIN-P.
           MOVE FUNCTION UPPER-CASE("i") TO R.
       END METHOD UPM.
       END FACTORY.
       END CLASS PB111CLS.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB111REV INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" THRU "A".
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RELM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  R               PIC X(4).
       PROCEDURE DIVISION RETURNING R.
       MAIN-P.
           IF "A" < "B" MOVE "LT" TO R ELSE MOVE "GT" TO R END-IF.
       END METHOD RELM.
       END OBJECT.
       END CLASS PB111REV.
