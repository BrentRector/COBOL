      *>--- conformance-case
      *> id: pb2937-skeleton-nested-edition-gate
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2937 (branch review R1-2) - an UNEXPANDED
      *> parameterized class binds its method bodies as itself (OO
      *> deep-dive D12 determination 6). The IF's condition reads an
      *> inline invocation through the formal-typed ITEM, so the IF is
      *> each expansion's; but the MOVE nested under it is the same in
      *> every expansion, and a multi-character ALL literal is not
      *> permitted to be associated with a numeric item (8.3.3.6.3
      *> SR3; cite.py --check 8.3.3.6.3 -> OK 8.3.3.6.3 3)), removed in
      *> COBOL-2002: COBOLNET0902 in the skeleton, whether or not
      *> anything expands the class. The deferred IF used to hide its
      *> branches from the edition pass, and this compiled clean.
       IDENTIFICATION DIVISION.
       CLASS-ID. NSKNEG INHERITS FROM BASE USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS T.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE T.
       01 CNT2 PIC 9.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           IF ITEM :: "SZ" = 1
               MOVE ALL "12" TO CNT2
           END-IF.
       END METHOD SHOW.
       END OBJECT.
       END CLASS NSKNEG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSKNEGM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM NSKNEGM.
