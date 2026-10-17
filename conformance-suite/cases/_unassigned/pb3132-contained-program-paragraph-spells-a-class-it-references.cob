*>--- conformance-case
*> id: pb3132-contained-program-paragraph-spells-a-class-it-references
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3132 (branch review R4-1, wave 1074) - ISO 8.3.2.2:
      *>   "Within a source element, a given user-defined word may be
      *>   used as only one type of user-defined word". A contained
      *>   program is a source element of its own (10.5). PB3132IN USES
      *>   PB3132X as an object-class-name (the USAGE OBJECT REFERENCE
      *>   PB3132X of its item; the class is declared by the CONTAINING
      *>   program's REPOSITORY, which applies to it by 12.3.4 GR1 and
      *>   lets it reference the class by 8.4.6.4) and then DECLARES
      *>   PB3132X as a paragraph-name: two types in PB3132IN's source
      *>   element, refused COBOLNET2692 at the paragraph, exactly as the
      *>   same shape is in the containing program itself. PB3132IN2
      *>   declares PB3132X as a paragraph-name and never references the
      *>   class: legal (8.4.6.1), and it must draw no diagnostic.
      *> cite.py --check 8.3.2.2 "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word" -> OK  8.3.2.2; 10.5 "A source element is a source unit
      *>   excluding any contained source units" -> OK  10.5; 12.3.4
      *>   "apply to each directly or indirectly contained source unit"
      *>   -> OK  12.3.4 1); 8.4.6.4 "shall be either the name of the
      *>   containing object class definition or declared in the
      *>   REPOSITORY paragraph of that or a containing source element"
      *>   -> OK  8.4.6.4; 8.4.6.1 "may use identical user-defined words"
      *>   -> OK  8.4.6.1
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3132X INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB3132X.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3132OUT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3132X.
       PROCEDURE DIVISION.
           CALL "PB3132IN".
           CALL "PB3132IN2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3132IN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB3132X.
       PROCEDURE DIVISION.
       PB3132X.
           DISPLAY "IN".
       END PROGRAM PB3132IN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3132IN2.
       PROCEDURE DIVISION.
       PB3132X.
           DISPLAY "IN2".
       END PROGRAM PB3132IN2.
       END PROGRAM PB3132OUT.
