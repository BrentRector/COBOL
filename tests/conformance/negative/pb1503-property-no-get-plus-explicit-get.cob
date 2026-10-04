      *> reject-at: 2002 2014 2023
      *> kb/Work PB1503 - ISO/IEC 1989:2023 section 11.7.3 SR5: "If property-name-1 is specified as a data-name in the
      *> working-storage section of the containing object definition, the PROPERTY clause shall not be specified in
      *> the data description entry of that data-name."
      *>   cite.py: OK  11.7.3 5)
      *> The rule is unconditional: WITH NO GET defines no implicit GET, yet the explicit GET PROPERTY BAL still names
      *> a data-name that carries the PROPERTY clause.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1503D INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(4) PROPERTY WITH NO GET.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           MOVE BAL TO LK-R.
       END METHOD.
       END OBJECT.
       END CLASS PB1503D.
