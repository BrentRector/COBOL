*>--- conformance-case
*> id: pb2077-refmod-into-elementary-by-reference
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2077 (decision PB3098) - a REFERENCE-MODIFIED argument passed BY REFERENCE to an
      *> ELEMENTARY formal of a method invoked through a CLASS-typed object reference. 8.4.3.3.4 GR6: "The
      *> unique data item is considered to be an elementary data item without the JUSTIFIED clause"
      *> (cite.py --check 8.4.3.3.4 -> OK 8.4.3.3.4 6)) - it has no PICTURE clause, and 9.3.6 match rule
      *> 3 e) requires "the same ALIGNED, ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE,
      *> SIGN, and USAGE clauses" (OK 9.3.6 3)), so X(1:5) never matches LP PIC X(5). 14.9.23.4 GR7 b)
      *> locates the method by 9.3.6 for every INVOKE (OK 14.9.23.4 7)); a typed receiver's descriptions
      *> are static, so the refusal is at compile time (COBOLNET0828; the universal lane's twin is
      *> EC-OO-METHOD in conformance:cases/_unassigned/pb2077_refmod_argument_both_lanes). Before the fix the typed lane
      *> accepted the equal-length slice and ran it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2077NE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2077NE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C2077NE.
       01 X  PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
           INVOKE C2077NE "NEW" RETURNING T
           INVOKE T "TP" USING X(1:5)
           STOP RUN.
       END PROGRAM PB2077NE.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2077NE INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2077NE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP PIC X(5).
       PROCEDURE DIVISION USING LP.
           DISPLAY "TP:" LP.
       END METHOD TP.
       END OBJECT.
       END CLASS C2077NE.
