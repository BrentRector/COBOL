*>--- conformance-case
*> id: pb2077-refmod-shorter-than-group-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2077 (decision PB3098) - a REFERENCE-MODIFIED argument passed BY REFERENCE to a GROUP
      *> formal LARGER than the slice, through a CLASS-typed object reference. The slice matches the group
      *> (neither carries any clause of 9.3.6 match rule 3 e), OK 9.3.6 3)), and 14.8.2.2 rule 1 then
      *> requires "the formal parameter shall be described with the same number or a smaller number of
      *> bytes as the corresponding argument" (cite.py --check 14.8.2.2 -> OK 14.8.2.2 1)); 14.9.23.3 SR5 c)
      *> applies 14.8.2 at compile time (OK 14.9.23.3 5) c)). X(1:3) is 3 bytes and LG 4: COBOLNET0828 (the
      *> universal lane's twin is EC-OO-UNIVERSAL in conformance:cases/_unassigned/pb2077_refmod_argument_both_lanes).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2077NS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2077NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C2077NS.
       01 X  PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
           INVOKE C2077NS "NEW" RETURNING T
           INVOKE T "TG" USING X(1:3)
           STOP RUN.
       END PROGRAM PB2077NS.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2077NS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2077NS.
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
       END CLASS C2077NS.
