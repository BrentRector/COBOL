*>--- conformance-case
*> id: pb2588-as-numeric-constant
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2588 - ISO 13.10.3 SR2 lets constant-name-1 stand only where a format
      *> specifies "a literal of the class and category of constant-name-1".  K2588-N is
      *> AS 5, so by 13.10.4 GR2 its class and category is numeric, and the PROGRAM-ID AS
      *> phrase specifies an alphanumeric or national literal (ISO 11.10.3 SR1: "Literal-1
      *> shall be an alphanumeric literal or a national literal"), so the word is refused
      *> exactly as the numeric literal 5 written there would be.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2588N AS K2588-N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2588-N CONSTANT AS 5.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM N2588N.
