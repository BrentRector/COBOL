      *> reject-at: 2002 2014 2023
      *> kb/Work PB1475 -- §8.5.3.1 exception 2): period and comma
      *> picture symbols match only when DECIMAL-POINT IS COMMA is in
      *> effect for both type declarations or for neither.
      *> cite.py --check 8.5.3.1 "Period picture symbols match if and
      *>   only if the DECIMAL-POINT IS COMMA clause is in effect for
      *>   both or for neither of these type declarations"
      *>   -> OK §8.5.3.1 2)
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK §14.8.2.2 2)
      *> Both T1 declarations write F PIC 9.99, but only the callee has
      *> DECIMAL-POINT IS COMMA, where '.' is an insertion character
      *> and 1.50 would read as 150,00. The declarations are therefore
      *> not equivalent, A and L are not of the same type, and the CALL
      *> is rejected (COBOLNET1688, call-argument-conformance). An
      *> implementation comparing the picture TEXT alone accepts it.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GDS IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       LINKAGE SECTION.
       01  T1 TYPEDEF STRONG.
           05  F           PIC 9.99.
       01  L TYPE T1.
       PROCEDURE DIVISION USING L.
       END PROGRAM W66GDS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GDC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM W66GDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 TYPEDEF STRONG.
           05  F           PIC 9.99.
       01  A TYPE T1.
       PROCEDURE DIVISION.
           MOVE 1.5 TO F OF A.
           CALL W66GDS USING A.
           STOP RUN.
       END PROGRAM W66GDC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GDS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 TYPEDEF STRONG.
           05  F           PIC 9.99.
       01  N               PIC 9(5)V99.
       LINKAGE SECTION.
       01  L TYPE T1.
       PROCEDURE DIVISION USING L.
           MOVE F OF L TO N.
           DISPLAY N.
           GOBACK.
       END PROGRAM W66GDS.
