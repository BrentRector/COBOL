*>--- conformance-case
*> id: exception_file_n
*> rules: [RV-15.31.3-1, FMT-15.29.2, FMT-15.31.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.29 EXCEPTION-FILE-N / 15.31 EXCEPTION-LOCATION-N — the national twins of the
      *> last-exception interrogation functions (P10 Step-11 EC-N wave; the only -N EC twins the
      *> 2023 text defines). Each returns the SAME content as its alphanumeric base converted to
      *> the runtime national character set (15.29.4 r1c / 15.31.3 r2b) — under D-N4 the Latin-1
      *> code points keep their values, so the observable proof is (a) the value, (b) FUNCTION
      *> LENGTH in character positions over the national result, and (c) the result CATEGORY:
      *> a national MOVE + N"" comparison is Table-16 legal only for a category-national source.
      *> Before any exception the no-EC-I-O case returns two national zeros (15.29.4 r1a).
      *> The AT END leg mirrors the EXCEPTION-FILE base net (9.1.13.1 status 10 -> EC-I-O-AT-END;
      *> 15.29.4 r1c = the I-O status + the SELECT-spelled file-name). The TURN lacks WITH
      *> LOCATION, so EXCEPTION-LOCATION-N is one national space (15.31.3 r1 — the documented
      *> 15.30.3-r1 implementor choice saves none), proven by LENGTH = 1.
>>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ECFNP10EN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "ecfn-p10en.dat".
       DATA DIVISION.
       FILE SECTION.
       FD TF.
       01 TF-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 N-RES  PIC N(6).
       01 LEN-R  PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-I-O-AT-END.
       EC-H-P.
           DISPLAY "S: " FUNCTION EXCEPTION-STATUS.
      *> 15.29.4 r1c: the I-O status (10) in national characters + the SELECT file-name (TF).
           DISPLAY "FN: " FUNCTION EXCEPTION-FILE-N.
           MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-FILE-N) TO LEN-R.
           DISPLAY "LEN-FN=" LEN-R.
      *> The result IS category national (15.29.1): MOVE to PIC N + compare with an N"" literal.
           MOVE FUNCTION EXCEPTION-FILE-N TO N-RES.
           IF N-RES = N"10TF" DISPLAY "NAT=YES" ELSE DISPLAY "NAT=NO".
      *> 15.31.3 r1: no WITH LOCATION on the enabling TURN -> one national space.
           MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-LOCATION-N) TO LEN-R.
           DISPLAY "LEN-LN=" LEN-R.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
      *> 15.29.4 r1a: the last exception status is not EC-I-O -> two national zeros.
           DISPLAY "PRE=" FUNCTION EXCEPTION-FILE-N.
           MOVE FUNCTION LENGTH(FUNCTION EXCEPTION-FILE-N) TO LEN-R.
           DISPLAY "LEN-PRE=" LEN-R.
           OPEN OUTPUT TF.
           MOVE "HELLO" TO TF-REC.
           WRITE TF-REC.
           CLOSE TF.
           OPEN INPUT TF.
           READ TF.
           READ TF.
           DISPLAY "AFTER".
           CLOSE TF.
           STOP RUN.
