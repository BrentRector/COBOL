      *> kb/Work PB1475 -- §8.5.3.1 type-declaration equivalence reads
      *> the LOCALE phrase by its EXTERNAL IDENTIFICATION, never by the
      *> locale-NAME that carries it.
      *> cite.py --check 8.5.3.1 "both specify the same SIZE phrase in
      *>   the LOCALE phrase of the PICTURE clause" -> OK §8.5.3.1 2)
      *> cite.py --check 8.5.3.1 "where the external identification is
      *>   the external-locale-name or literal value associated with a
      *>   locale-name" -> OK §8.5.3.1 2)
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK §14.8.2.2 2)
      *> The caller names its locale LA, the callee LB; both are bound
      *> to the literal "fr_FR" and both pictures say SIZE 16, so the
      *> two T1 declarations are equivalent, A and L are of the same
      *> type and the CALL conforms. (The negative twin for the other
      *> half of the rule is negative/w66g-pb1475-strong-type-dpc-differs.)
      *> DERIVATION: the callee moves 42 to G OF L, which IS the
      *> caller's A (BY REFERENCE) -> G=042.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GLC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE LA IS "fr_FR".
       REPOSITORY.
           PROGRAM W66GLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 TYPEDEF STRONG.
           05  F           PIC +$9(5).99 LOCALE LA SIZE 16.
           05  G           PIC 9(3).
       01  A TYPE T1.
       PROCEDURE DIVISION.
           CALL W66GLS USING A.
           DISPLAY "G=" G OF A.
           STOP RUN.
       END PROGRAM W66GLC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66GLS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE LB IS "fr_FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T1 TYPEDEF STRONG.
           05  F           PIC +$9(5).99 LOCALE LB SIZE 16.
           05  G           PIC 9(3).
       LINKAGE SECTION.
       01  L TYPE T1.
       PROCEDURE DIVISION USING L.
           MOVE 42 TO G OF L.
           GOBACK.
       END PROGRAM W66GLS.
