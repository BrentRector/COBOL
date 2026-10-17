*>--- conformance-case
*> id: pb3128-refmod-dynamic-limit-below-group-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3128 - a REFERENCE-MODIFIED slice of a DYNAMIC LENGTH item whose LIMIT is SMALLER than
      *> a GROUP formal, through a CLASS-typed object reference. 8.5.1.10.1 bounds the item's length by
      *> its maximum size, the smallest of "the value declared in the LIMIT phrase" and the implementor's
      *> maximum (OK 8.5.1.10.1), and 8.5.1.10.4 makes a reference-modified one a fixed-length item of its
      *> current length (OK 8.5.1.10.4), so DL(1:N) is never more than 3 bytes. 14.8.2.2 rule 1 requires
      *> "the formal parameter shall be described with the same number or a smaller number of bytes as the
      *> corresponding argument" (OK 14.8.2.2 1)), measured for a run-time slice by its MAXIMUM extent
      *> (docs/CONFORMANCE.md D-RM1 a)); 14.9.23.3 SR5 c) applies 14.8.2 at compile time. LG is 4 bytes:
      *> COBOLNET0828 "larger than the argument (3)". The unlimited twin is admitted:
      *> conformance:2014/pb3128_refmod_dynamic_length_argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3128NL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C3128NL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T  USAGE OBJECT REFERENCE C3128NL.
       01 DL PIC X DYNAMIC LENGTH LIMIT 3.
       01 N  PIC 9(2) VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE C3128NL "NEW" RETURNING T
           MOVE "ABC" TO DL
           INVOKE T "TG" USING DL(1:N)
           STOP RUN.
       END PROGRAM PB3128NL.

       IDENTIFICATION DIVISION.
       CLASS-ID. C3128NL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C3128NL.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           MOVE "wxyz" TO LG.
       END METHOD TG.
       END OBJECT.
       END CLASS C3128NL.
