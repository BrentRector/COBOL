      *> reject-at: 2002 2014 2023
      *> kb/Work PB988 - ISO/IEC 1989:2023 section 10.7.3 SR5:
      *>   "If the PROPERTY phrase is specified in the METHOD-ID
      *>   paragraph, method-name-1 shall be omitted."
      *>   cite.py: OK  10.7.3 5)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB988K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-V PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. GET PROPERTY V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9.
       PROCEDURE DIVISION RETURNING LK-V.
       GMAIN.
           MOVE W-V TO LK-V.
       END METHOD V.
       END OBJECT.
       END CLASS PB988K.
