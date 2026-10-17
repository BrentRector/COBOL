*>--- conformance-case
*> id: pb892-operand-activation-below-2002
*> rules: [GR-14.9.18.4-1]
*> source-format: free
*> portable: unverified
*>---
      *> The REJECT half of conformance:2002/pb892_operand_activation_resume
      *> (kb/Work PB892). A condition propagated from an activation written
      *> as an OPERAND needs a user-defined function (ISO 9.4 / 11.5), the
      *> GOBACK RAISING phrase (14.9.18) and USE AFTER EXCEPTION CONDITION
      *> - all ISO/IEC 1989:2002 introductions. At COBOL-85 this source does
      *> not describe a program, so the compiler refuses the FIRST construct
      *> the edition does not have with COBOLNET0900 rather than activating
      *> the function and dropping the condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB892N85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB892N85F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 99 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE X = FUNCTION PB892N85F + 1.
           DISPLAY "X=" X.
           STOP RUN.
       END PROGRAM PB892N85.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB892N85F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R RAISING EC-USER-PBN.
       F-P.
           MOVE 7 TO R.
           GOBACK RAISING EXCEPTION EC-USER-PBN.
       END FUNCTION PB892N85F.
