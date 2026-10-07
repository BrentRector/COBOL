      *> reject-at: 85
      *> kb/Work PB2076 - the edition floor of the positive golden
      *> 2002/pb2076_universal_returning_move_match: an INVOKE (14.9.23)
      *> through a universal object reference, a CLASS-ID and USAGE
      *> OBJECT REFERENCE are COBOL 2002 features, so the 9.3.6 match
      *> rule 7 crossing cannot be written at --std 85 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2076N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2076N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 RX PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE C2076N "NEW" RETURNING U
           INVOKE U "RN" RETURNING RX
           STOP RUN.
       END PROGRAM PB2076N.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2076N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9V99.
       PROCEDURE DIVISION RETURNING LN.
           MOVE 1.25 TO LN.
       END METHOD RN.
       END OBJECT.
       END CLASS C2076N.
