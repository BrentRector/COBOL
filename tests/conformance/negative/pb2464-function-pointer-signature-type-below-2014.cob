      *> reject-at: 85 2002
      *> kb/Work PB2464 - the edition floor of the positive golden
      *> 2014/pb2464_function_pointer_signature_type: USAGE
      *> FUNCTION-POINTER (13.18.60.4 GR26) is a COBOL 2014 category, so a
      *> function-pointer restricted to a function-prototype cannot be
      *> declared below --std 2014 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F1N IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION F1N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2464G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F1N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP1 USAGE FUNCTION-POINTER TO F1N.
       PROCEDURE DIVISION.
       MAIN-P.
           SET FP1 TO NULL
           STOP RUN.
       END PROGRAM PB2464G.
