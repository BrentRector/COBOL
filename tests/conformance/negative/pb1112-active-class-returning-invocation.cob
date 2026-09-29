      *> reject-at: 2002 2014 2023
      *> kb/Work PB1112 -- ISO 14.8.3.3 rule 2: an ACTIVE-CLASS returning
      *> item is delivered by the SET rules with a sending operand
      *> described by the INVOCATION -- b) 4. "If the activated method is
      *> invoked with any other object reference, the sending operand has
      *> the same description as that object reference". N invokes MR
      *> through O2, described C1 (no ONLY), so the sender is described
      *> C1, and 14.9.39.3 SR14 admits into the ACTIVE-CLASS receiver Z
      *> only an ACTIVE-CLASS reference, SELF or NULL -- COBOLNET0828.
      *> (Before the fix the sender was always read as ACTIVE-CLASS and
      *> this compiled.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C12T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE C12T.
       PROCEDURE DIVISION.
           INVOKE C12T "NEW" RETURNING O
           INVOKE O "N"
           STOP RUN.
       END PROGRAM PB12T.
       IDENTIFICATION DIVISION.
       CLASS-ID. C12T INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF.
       END METHOD MR.
       METHOD-ID. N.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 Z USAGE OBJECT REFERENCE ACTIVE-CLASS.
       01 O2 USAGE OBJECT REFERENCE C12T.
       PROCEDURE DIVISION.
           SET O2 TO SELF
           INVOKE O2 "MR" RETURNING Z.
       END METHOD N.
       END OBJECT.
       END CLASS C12T.
