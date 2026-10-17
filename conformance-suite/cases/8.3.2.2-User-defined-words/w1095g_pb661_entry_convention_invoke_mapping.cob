*>--- conformance-case
*> id: w1095g_pb661_entry_convention_invoke_mapping
*> rules: [GR-8.3.2.2-L4.2, DOC-A.1-68]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB661 - ISO §8.3.2.2, the INVOKE method-name mapping
      *> under a class whose entry convention is STATED, through both
      *> kinds of object reference.
      *>   cite.py --check 8.3.2.2 "the naming convention and mapping to
      *>     be used for the method-name is determined by the entry
      *>     convention of the class or interface that contains the
      *>     method"  -> OK §8.3.2.2 2)
      *>   cite.py --check 8.3.2.2 "the ENTRY-CONVENTION, if any, in the
      *>     class or interface definition containing the method is
      *>     ignored"  -> OK §8.3.2.2 2)
      *>   cite.py --check 11.9.7.4 "When COBOL is specified, the naming
      *>     convention and mapping of method-names and program-names
      *>     are as specified in 8.3.2.2"  -> OK §11.9.7.4 2)
      *> The mapping §8.3.2.2 leaves to the implementor is documented in
      *> docs/CONFORMANCE.md DOC-A.1-68: two externalized names are the
      *> same name when they are equal ignoring case, and a method with
      *> an AS phrase is reached by its AS name, never by its declared
      *> word. Derivation of each expected line:
      *>   M2 RAN   T is typed W1095GC, whose OPTIONS paragraph states
      *>            ENTRY-CONVENTION IS COBOL, so the class's convention
      *>            decides the mapping (§11.9.7.4 GR2 -> §8.3.2.2):
      *>            "MIXEDEXT" equals the AS name "MixedExt" ignoring
      *>            case and reaches M2.
      *>   M2 RAN   U is universal, so the class's ENTRY-CONVENTION is
      *>            ignored and the implied COBOL call convention maps
      *>            "mixedext" as a COBOL word to the same AS name.
      *>   HANDLED=EC-OO-METHOD  "M2" is the declared word, which is no
      *>            externalized name; nothing is resolved and
      *>            EC-OO-METHOD is set (§9.3.6 6)), which the declarative
      *>            selects; the status is 31 characters, left-justified
      *>            (§15.33.3 rule 1).
      *>   DONE     execution resumes at the next statement.
       >>TURN EC-OO-METHOD CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1095GM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W1095GC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 T  USAGE OBJECT REFERENCE W1095GC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-METHOD.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE W1095GC "NEW" RETURNING T.
           SET U TO T.
           INVOKE T "MIXEDEXT".
           INVOKE U "mixedext".
           INVOKE U "M2".
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM W1095GM.

       IDENTIFICATION DIVISION.
       CLASS-ID. W1095GC INHERITS FROM BASE.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M2 AS "MixedExt".
       PROCEDURE DIVISION.
           DISPLAY "M2 RAN".
       END METHOD M2.
       END OBJECT.
       END CLASS W1095GC.
