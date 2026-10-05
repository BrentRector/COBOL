      *> reject-at: 2002 2014 2023
      *> kb/Work PB1744 - ISO 12.3.8.3 SR13 (cite.py OK): "If ALL is specified in
      *> the intrinsic format of the function-specifier, none of the names of the
      *> intrinsic functions may be specified as a user-defined word within the
      *> scope of this REPOSITORY paragraph." A parameter-name is a user-defined
      *> word (8.3.2.2), and SQRT is an intrinsic-function-name.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1744PN INHERITS FROM BASE USING SQRT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS SQRT
           FUNCTION ALL INTRINSIC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1744PN.
