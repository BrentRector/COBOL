*>--- conformance-case
*> id: pb2077-elementary-into-group-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2077 (decision PB3098, the sibling the settlement found) - a whole elementary PIC X(10)
      *> argument passed BY REFERENCE to a GROUP formal of a method invoked through a CLASS-typed object
      *> reference. 9.3.6 match rule 3 e) requires "the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC
      *> LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" (cite.py --check 9.3.6 -> OK 9.3.6 3)): the
      *> argument has a PICTURE clause and the group formal none, so the method does not match - asked of
      *> every INVOKE by 14.9.23.4 GR7 b) (OK 14.9.23.4 7)). The typed lane refuses it at compile time
      *> (COBOLNET0828); the universal lane raises EC-OO-METHOD (conformance:2002/
      *> pb480_universal_match_relations, pb2077_refmod_argument_both_lanes). Before the fix the typed lane
      *> asked 14.8.2.2 rule 1 alone and ran it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2077NG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2077NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C2077NG.
       01 X  PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
           INVOKE C2077NG "NEW" RETURNING T
           INVOKE T "TG" USING X
           STOP RUN.
       END PROGRAM PB2077NG.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2077NG INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2077NG.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG.
       END METHOD TG.
       END OBJECT.
       END CLASS C2077NG.
