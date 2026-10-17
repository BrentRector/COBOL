      *>--- conformance-case
      *> id: l1c21_property_clause_format
      *> rules: [FMT-13.18.42.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.42.2 format — every PROPERTY clause spelling works
      *> General format: PROPERTY [ WITH NO { GET | SET } ] [ IS FINAL ]
      *> with PROPERTY, NO, GET, SET, FINAL underlined (required) and
      *> WITH, IS not underlined (optional words, §5.2.3;
      *> cite.py --check 5.2.3 -> OK  §5.2.3 (Optional words)).
      *>   cite.py --check 13.18.42.2 "PROPERTY" -> OK (General format)
      *> Semantics each spelling must carry:
      *>   §13.18.42.4 GR1 "If the GET phrase is not specified, the
      *>   PROPERTY clause causes a method to be defined for the
      *>   containing object." (the get property method)
      *>   cite.py --check 13.18.42.4 -> OK  §13.18.42.4 1)  (General
      *>   rules)
      *>   §13.18.42.4 GR2 "If the SET phrase is not specified, the
      *>   PROPERTY clause causes a method to be defined for the
      *>   containing object." (the set property method)
      *>   cite.py --check 13.18.42.4 -> OK  §13.18.42.4 2)  (General
      *>   rules)
      *>   §8.4.3.9.3 SR1 "Property-name-1 shall be an object property
      *>   specified in the REPOSITORY paragraph." (hence the four
      *>   PROPERTY entries in L1C21L's REPOSITORY)
      *>   cite.py --check 8.4.3.9.3 -> OK  §8.4.3.9.3 1)  (Syntax
      *>   rules)
      *> The properties live in the FACTORY working-storage (§13.18.42.3
      *> SR1 permits it), so the factory object is used and no instance
      *> is ever created. The four spellings:
      *>   PGET  PROPERTY NO SET              (WITH omitted)   get only
      *>   PSET  PROPERTY WITH NO GET IS FINAL (full, GET)     set only
      *>   PBOTH PROPERTY FINAL               (IS omitted)     get + set
      *>   PFIN  PROPERTY WITH NO SET IS FINAL (full, SET)     get only
      *> Expected output, derived (PIC 9(3) unsigned, DISPLAY shows all
      *> three digits):
      *>   "PGET 011"  — get method of PGET returns its VALUE 11.
      *>   "PSET 005"  — MOVE 5 to PSET invokes its set method; the
      *>                 factory method SHOWSET displays the item (there
      *>                 is no get method to read it through).
      *>   "PBOTH 007" — set method stores 7, get method returns it.
      *>   "PFIN 044"  — get method of PFIN returns its VALUE 44.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21L.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1C21P
           PROPERTY PGET
           PROPERTY PSET
           PROPERTY PBOTH
           PROPERTY PFIN.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PGET " PGET OF L1C21P
           MOVE 5 TO PSET OF L1C21P
           INVOKE L1C21P "SHOWSET"
           MOVE 7 TO PBOTH OF L1C21P
           DISPLAY "PBOTH " PBOTH OF L1C21P
           DISPLAY "PFIN " PFIN OF L1C21P
           STOP RUN.
       END PROGRAM L1C21L.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C21P.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PGET  PIC 9(3) VALUE 11 PROPERTY NO SET.
       01 PSET  PIC 9(3) VALUE 22 PROPERTY WITH NO GET IS FINAL.
       01 PBOTH PIC 9(3) VALUE 33 PROPERTY FINAL.
       01 PFIN  PIC 9(3) VALUE 44 PROPERTY WITH NO SET IS FINAL.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWSET.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PSET " PSET.
       END METHOD SHOWSET.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C21P.
