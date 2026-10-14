      *> reject-at: 2002 2014 2023
      *> kb/Work PB3132 - ISO 8.3.2.2: "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word". XB below is the object-class-name of its class AND the
      *>   method-name of a method of that class whose body also USES the
      *>   class-name (INVOKE XB). The one declaration funnel refuses the
      *>   second declaration COBOLNET2692; the word is not exempt (the
      *>   exceptions are a compilation-variable-name, a level-number
      *>   that matches a paragraph-name or section-name, and the
      *>   constant-name / data-name / property-name / record-key-name /
      *>   record-name group). The note claimed this shape was not
      *>   refused: re-probed, it is, and this negative pins it.
      *> cite.py --check 8.3.2.2 "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word" -> OK  8.3.2.2
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
