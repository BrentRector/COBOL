*>--- conformance-case
*> id: pb780-call-numeric-constant
*> rules: [SR-13.10.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 - a constant-name is admitted where CALL writes literal-1 (13.10.3 SR2) and then meets the
      *> rules of the literal it stands for (13.10.4 GR1 "as if literal-1 ... were written"): 14.9.4.3 SR2 "Literal-1
      *> shall be an alphanumeric or national literal", and K9 stands for the NUMERIC literal 42. The alphanumeric
      *> form is conformance/2002/pb780_constant_names_name_programs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780CN9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K9 CONSTANT AS 42.
       PROCEDURE DIVISION.
           CALL K9.
           STOP RUN.
