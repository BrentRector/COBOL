*>--- conformance-case
*> id: pb3132-method-id-spells-its-class-id-and-invokes-it
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3132 - ISO 8.3.2.2: "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word". A method is a source element of its own (10.5), so its
      *>   METHOD-ID may spell its CLASS-ID (kb/Work PB2921, golden
      *>   2002/pb2921_class_named_method); but this method's body also
      *>   USES PB3132X as an object-class-name (INVOKE PB3132X), so its
      *>   own source element uses the word as a method-name AND an
      *>   object-class-name. The method's census records the class-name
      *>   reference (OoClassTable.RecordReferenceUse) and refuses it
      *>   COBOLNET2692 at the INVOKE; the word is not exempt (the
      *>   exceptions are a compilation-variable-name, a level-number
      *>   that matches a paragraph-name or section-name, and the
      *>   constant-name / data-name / property-name / record-key-name /
      *>   record-name group).
      *> cite.py --check 8.3.2.2 "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word" -> OK  8.3.2.2; 10.5 "A source element is a source unit
      *>   excluding any contained source units" -> OK  10.5
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3132X INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB3132X.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PB3132X.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB3132X "NEW" RETURNING R.
       END METHOD PB3132X.
       END OBJECT.
       END CLASS PB3132X.
