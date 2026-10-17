*>--- conformance-case
*> id: pb3132-method-paragraph-spells-a-class-it-references
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3132 - ISO 8.3.2.2: "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word". A method is a source element of its own (10.5). Method
      *>   M2 USES PB3132D as an object-class-name (the USAGE OBJECT
      *>   REFERENCE PB3132D of its local item; the class is declared by
      *>   the class's REPOSITORY, 8.4.6.4) and then DECLARES PB3132D as a
      *>   paragraph-name: two types in M2's source element, refused
      *>   COBOLNET2692 at the paragraph (the use came first, so the
      *>   declaration funnel is the one that sees the conflict). Method M3
      *>   declares PB3132D as a data-name and never references the
      *>   class: legal (8.4.6.1), and it must draw no diagnostic.
      *> cite.py --check 8.3.2.2 "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word" -> OK  8.3.2.2; 10.5 "A source element is a source unit
      *>   excluding any contained source units" -> OK  10.5; 8.4.6.4
      *>   "shall be either the name of the containing object class
      *>   definition or declared in the REPOSITORY paragraph of that or a
      *>   containing source element" -> OK  8.4.6.4; 8.4.6.1 "each source
      *>   element may use identical user-defined words to name items
      *>   independent of the use of these user-defined words by other
      *>   source elements" -> OK  8.4.6.1
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3132C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB3132D.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M2.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB3132D.
       PROCEDURE DIVISION.
       PB3132D.
           CONTINUE.
       END METHOD M2.
       METHOD-ID. M3.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 PB3132D PIC X.
       PROCEDURE DIVISION.
           DISPLAY PB3132D.
       END METHOD M3.
       END OBJECT.
       END CLASS PB3132C.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3132D INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB3132D.
