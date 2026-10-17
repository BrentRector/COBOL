      *>--- conformance-case
      *> id: pb2921_class_named_method
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2921 - each method is a source element of its own,
      *> so its words may repeat a word of its class or of a sibling
      *> method as another type of user-defined word.
      *> ISO 8.3.2.2: "Within a source element, a given user-defined
      *>   word may be used as only one type of user-defined word"
      *>   -> OK 8.3.2.2; 10.5: "A source element is a source unit
      *>   excluding any contained source units" -> OK 10.5; 8.4.6.1:
      *>   each source element "may use identical user-defined words
      *>   to name items independent of the use of these user-defined
      *>   words by other source elements" -> OK 8.4.6.1; 8.4.6.5:
      *>   "The method-name of a method is declared in the METHOD-ID
      *>   paragraph" -> OK 8.4.6.5.
      *> So METHOD-ID PB2921C may share the class-name PB2921C, and the
      *>   condition-name Q of method PB2921C-M may share the data-name
      *>   Q of method PB2921C. The compiler kept ONE census for a class
      *>   and all its methods and refused both COBOLNET2692; and it
      *>   rendered the class-named method with an _M suffix, the C#
      *>   identifier of the method-name PB2921C-M (CS0111).
      *> Expected values: each INVOKE names one method, and each
      *>   method prints its own name and its own Q, once per call.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2921M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2921C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB2921C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB2921C "NEW" RETURNING OBJ
           INVOKE OBJ "PB2921C"
           INVOKE OBJ "PB2921C-M"
           INVOKE OBJ "PB2921C"
           STOP RUN.
       END PROGRAM PB2921M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2921C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PB2921C.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 Q PIC X VALUE "D".
       PROCEDURE DIVISION.
           DISPLAY "PB2921C " Q.
       END METHOD PB2921C.
       METHOD-ID. PB2921C-M.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 R PIC X VALUE "C".
          88 Q VALUE "C".
       PROCEDURE DIVISION.
           IF Q
               DISPLAY "PB2921C-M Q"
           END-IF.
       END METHOD PB2921C-M.
       END OBJECT.
       END CLASS PB2921C.
