*>--- conformance-case
*> id: pb3154-object-data-spells-a-class-it-references
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3154 - ISO 8.3.2.2: "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word". The object definition is a source element of its own
      *>   (10.5), so its data-name may spell a class the CLASS
      *>   definition's REPOSITORY names (golden
      *>   2002/pb3154_half_data_named_like_class_words); but this object
      *>   definition's own data division also USES PB3154P as an
      *>   object-class-name (USAGE OBJECT REFERENCE PB3154P), so its
      *>   source element uses the word as a data-name AND an
      *>   object-class-name. The object definition's census records the
      *>   class-name reference (OoClassTable.RecordReferenceUse) and
      *>   refuses it COBOLNET2692; the word is not exempt (the
      *>   exceptions are a compilation-variable-name, a level-number
      *>   that matches a paragraph-name or section-name, and the
      *>   constant-name / data-name / property-name / record-key-name /
      *>   record-name group).
      *> cite.py --check 8.3.2.2 "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word" -> OK  8.3.2.2; 10.5 "A source element is a source unit
      *>   excluding any contained source units" -> OK  10.5
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3154P INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB3154P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3154Q INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB3154P.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB3154P PIC X.
       01 R USAGE OBJECT REFERENCE PB3154P.
       END OBJECT.
       END CLASS PB3154Q.
