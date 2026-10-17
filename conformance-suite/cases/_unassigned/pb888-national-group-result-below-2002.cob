*>--- conformance-case
*> id: pb888-national-group-result-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB888 - the NEGATIVE twin of
      *> tests/conformance/2002/pb888_compiler_temp_takes_the_whole_description. The temporary whose
      *> description that golden pins exists only for a user-defined function (ISO 8.4.3.2.4, FUNCTION-ID
      *> 11.5) returning a GROUP-USAGE NATIONAL group (13.18.29) - COBOL-2002 introductions both - so
      *> below 2002 the construct is refused (COBOLNET0900) rather than bound with any temporary at all.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB888NGNEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RESULT-NG GROUP-USAGE NATIONAL.
          02 RN PIC N(4).
       PROCEDURE DIVISION RETURNING RESULT-NG.
           MOVE N"WXYZ" TO RN
           GOBACK.
       END FUNCTION PB888NGNEG.
