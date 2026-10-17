      *>--- conformance-case
      *> id: pb2921_data_named_like_method
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2921 (review R2-1) - an object or factory data-name
      *> may be spelled like a METHOD-ID of the same definition.
      *> ISO 8.4.6.5: "The method-name of a method is declared in the
      *>   METHOD-ID paragraph" -> OK 8.4.6.5; 10.5: "A source element
      *>   is a source unit excluding any contained source units"
      *>   -> OK 10.5; 8.3.2.2: "Within a source element, a given
      *>   user-defined word may be used as only one type of
      *>   user-defined word" -> OK 8.3.2.2.
      *> So the method-name PQ (the method's source element) and the
      *>   object data-name PQ (the object definition's) never meet in
      *>   one source element; the factory pair FQ likewise. The data
      *>   field and the method shared one C# member name in the one
      *>   emitted class (CS0102) before this.
      *> Expected values: each method prints its literal, and SHOW /
      *>   FSHOW print the data item's VALUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2921DM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2921DC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB2921DC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB2921DC "NEW" RETURNING OBJ
           INVOKE OBJ "PQ"
           INVOKE OBJ "SHOW"
           INVOKE PB2921DC "FQ"
           INVOKE PB2921DC "FSHOW"
           STOP RUN.
       END PROGRAM PB2921DM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2921DC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FQ PIC X VALUE "F".
       PROCEDURE DIVISION.
       METHOD-ID. FQ.
       PROCEDURE DIVISION.
           DISPLAY "METHOD FQ".
       END METHOD FQ.
       METHOD-ID. FSHOW.
       PROCEDURE DIVISION.
           DISPLAY "DATA " FQ.
       END METHOD FSHOW.
       END FACTORY.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PQ PIC X VALUE "D".
       PROCEDURE DIVISION.
       METHOD-ID. PQ.
       PROCEDURE DIVISION.
           DISPLAY "METHOD PQ".
       END METHOD PQ.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "DATA " PQ.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2921DC.
