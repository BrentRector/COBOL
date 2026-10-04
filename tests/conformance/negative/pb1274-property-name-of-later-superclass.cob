      *> reject-at: 2002 2014 2023
      *> kb/Work PB1274 - ISO/IEC 1989:2023 section 13.18.42.3 SR4: "The data-name for the subject of the entry shall
      *> not be the same as a property-name defined in a superclass."
      *>   cite.py: OK  13.18.42.3 4)
      *> The subclass is written BEFORE its superclass; both describe N with the PROPERTY clause.  The rule is about the
      *> class hierarchy, not the order of the source, so it is refused whichever class comes first.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1274OS INHERITS FROM PB1274OB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1274OB.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1274OS.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1274OB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1274OB.
