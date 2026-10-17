      *>--- conformance-case
      *> id: pb2463-universal-returning-set-match-below-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2463 - the edition floor of the positive golden
      *> 2002/pb2463_universal_returning_set_match: an INVOKE (14.9.23)
      *> through a universal object reference, a CLASS-ID and USAGE
      *> OBJECT REFERENCE are COBOL 2002 features, so the 9.3.6 match
      *> rule 6 crossing cannot be written at --std 85 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2463N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2463N
           CLASS C2463Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 OQ USAGE OBJECT REFERENCE C2463Q.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE C2463N "NEW" RETURNING U
           INVOKE U "RN" RETURNING OQ
           STOP RUN.
       END PROGRAM PB2463N.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463Q INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS C2463Q.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2463N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS C2463N.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN USAGE OBJECT REFERENCE C2463N.
       PROCEDURE DIVISION RETURNING LN.
           SET LN TO SELF.
       END METHOD RN.
       END OBJECT.
       END CLASS C2463N.
