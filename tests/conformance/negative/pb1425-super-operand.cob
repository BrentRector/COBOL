      *> reject-at: 2002 2014 2023
      *> kb/Work PB1425 - ISO 8.4.3.8.3 SR3: "SUPER may be specified only as the object in an object-property
      *>   identifier or as the object used to invoke a method with the INVOKE statement or an inline invocation
      *>   of a method." SUPER as a relation operand is neither, so it is refused by SR3 (COBOLNET2900) - never by
      *>   a parse error, now that SELF and SUPER are identifiers of the expression tier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425SM.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM PB1425SM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425SA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. CMP.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           SET O TO SELF
           IF O = SUPER DISPLAY "EQ" END-IF.
       END METHOD CMP.
       END OBJECT.
       END CLASS PB1425SA.
