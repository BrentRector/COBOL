*>--- conformance-case
*> id: pb2588_as_constant_name
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2588 - a constant-name as the operand of the AS externalized-name phrase.
      *> ISO 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere
      *> that a format specifies a literal of the class and category of constant-name-1", and
      *> 13.10.4 GR1: "the effect of specifying constant-name-1 in other than this entry is as if
      *> literal-1 ... were written where constant-name-1 is written".  Every AS position prints
      *> `AS literal-n` (11.5.2 FUNCTION-ID, 11.7.2 METHOD-ID, 11.10.2 PROGRAM-ID, 12.3.8.2 the
      *> REPOSITORY specifiers, 13.18.22.2 the EXTERNAL clause), so each admits an alphanumeric
      *> constant-name, and the name it externalizes is the constant's literal.  The constant
      *> entry stands LATER, in the DATA DIVISION, so the readers that form a name before the
      *> data division binds (the unit builder, the class table, the OO repository scope) read
      *> it from the source; the binder's readers bind it on demand.  Every name below is
      *> reachable ONLY through the constant's literal, so each line proves one position:
      *>   CALLEE HELLO  CALL DOIT - REPOSITORY PROGRAM DOIT AS K2588-PR names the prototype
      *>                 externalized as "P2588-CALLEE" (12.3.8.4 GR10), which reaches
      *>                 PROGRAM-ID P2588CB AS K2588-CB ("P2588-CALLEE", its own WORKING-STORAGE
      *>                 constant); HELLO is the main program's store into M-SHR, EXTERNAL AS
      *>                 K2588-EX ("P2588-SHARED"), read through the callee's C-SHR, EXTERNAL AS
      *>                 the literal "P2588-SHARED" (13.18.22.4 GR5: literal-1 is the name of the
      *>                 record that is externalized)
      *>   CALLEE HELLO  CALL "P2588-CALLEE" - 14.9.4.4 GR3 b) matches the externalized name
      *>   TWICE=042     FUNCTION TWICE AS K2588-TW ("P2588-TWICE") reaches FUNCTION-ID F2588 AS
      *>                 K2588-FN, the same name from the function's own constant (12.3.8.4 GR11)
      *>   SPOKEN        INVOKE ... "P2588-SPEAK" reaches METHOD-ID SPEAK AS K2588-MD, a constant
      *>                 of the containing OBJECT definition (11.7.4 GR5: a method sees the data
      *>                 division of its containing object definition); BOXY AS K2588-BOX names
      *>                 the class C2588BOX AS "P2588-BOX" (12.3.8.4 GR2)
      *>   SHOUTED       INVOKE ... "P2588-SHOUT" reaches METHOD-ID SHOUT AS K2588-SH, a constant of
      *>                 the method's own LOCAL-STORAGE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2588PR AS "P2588-CALLEE" IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM P2588PR.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. F2588 AS K2588-FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2588-FN CONSTANT AS "P2588-TWICE".
       LINKAGE SECTION.
       01  L-RES PIC 9(3).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 42 TO L-RES
           GOBACK.
       END FUNCTION F2588.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2588BOX AS "P2588-BOX" INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2588-MD CONSTANT AS "P2588-SPEAK".
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SPEAK AS K2588-MD.
       PROCEDURE DIVISION.
           DISPLAY "SPOKEN"
           GOBACK.
       END METHOD SPEAK.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOUT AS K2588-SH.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01  K2588-SH CONSTANT AS "P2588-SHOUT".
       PROCEDURE DIVISION.
           DISPLAY "SHOUTED"
           GOBACK.
       END METHOD SHOUT.
       END OBJECT.
       END CLASS C2588BOX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2588MN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOXY AS K2588-BOX
           FUNCTION TWICE AS K2588-TW
           PROGRAM DOIT AS K2588-PR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2588-BOX CONSTANT AS "P2588-BOX".
       01  K2588-TW  CONSTANT AS "P2588-TWICE".
       01  K2588-PR  CONSTANT AS "P2588-CALLEE".
       01  K2588-EX  CONSTANT AS "P2588-SHARED".
       01  M-SHR PIC X(5) EXTERNAL AS K2588-EX.
       01  B USAGE OBJECT REFERENCE BOXY.
       01  R PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "HELLO" TO M-SHR
           CALL DOIT
           CALL "P2588-CALLEE"
           COMPUTE R = FUNCTION TWICE
           DISPLAY "TWICE=" R
           INVOKE BOXY "NEW" RETURNING B
           INVOKE B "P2588-SPEAK"
           INVOKE B "P2588-SHOUT"
           STOP RUN.
       END PROGRAM P2588MN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2588CB AS K2588-CB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2588-CB CONSTANT AS "P2588-CALLEE".
       01  C-SHR PIC X(5) EXTERNAL AS "P2588-SHARED".
       PROCEDURE DIVISION.
       CB-P.
           DISPLAY "CALLEE " C-SHR
           GOBACK.
       END PROGRAM P2588CB.
