*>--- conformance-case
*> id: pb3132_contained_program_word_it_never_references
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3132 (branch review R4-1, wave 1074) - the legal half
      *>   of the contained-program arm. ISO 8.3.2.2: "Within a source
      *>   element, a given user-defined word may be used as only one
      *>   type of user-defined word". A contained program is a source
      *>   element of its own (10.5), and each element "may use
      *>   identical user-defined words" (8.4.6.1). PB3132PIN never
      *>   references the class PB3132Y its container's REPOSITORY
      *>   declares, so its paragraph PB3132Y is its only use of the word
      *>   and is legal; the container itself uses PB3132Y only as the
      *>   object-class-name. Expected output: the container's INVOKE
      *>   "NEW" sets R (an object reference is never NULL after it), so
      *>   "OBJ OK", then the contained program performs its paragraph,
      *>   "PARA PB3132Y", then "DONE".
      *> cite.py --check 8.3.2.2 "Within a source element, a given
      *>   user-defined word may be used as only one type of user-defined
      *>   word" -> OK  8.3.2.2; 10.5 "A source element is a source unit
      *>   excluding any contained source units" -> OK  10.5; 8.4.6.1
      *>   "may use identical user-defined words" -> OK  8.4.6.1
       IDENTIFICATION DIVISION.
       CLASS-ID. PB3132Y INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB3132Y.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3132POUT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3132Y.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R USAGE OBJECT REFERENCE PB3132Y.
       PROCEDURE DIVISION.
           INVOKE PB3132Y "NEW" RETURNING R.
           IF R NOT = NULL
               DISPLAY "OBJ OK"
           END-IF.
           CALL "PB3132PIN".
           DISPLAY "DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3132PIN.
       PROCEDURE DIVISION.
           PERFORM PB3132Y.
           GOBACK.
       PB3132Y.
           DISPLAY "PARA PB3132Y".
       END PROGRAM PB3132PIN.
       END PROGRAM PB3132POUT.
