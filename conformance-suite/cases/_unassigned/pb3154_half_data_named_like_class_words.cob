*>--- conformance-case
*> id: pb3154_half_data_named_like_class_words
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3154 - a factory or instance data item may be spelled
      *>   like a word the CLASS definition declares: a class its
      *>   REPOSITORY names, an alphabet-name its SPECIAL-NAMES declares,
      *>   or its own CLASS-ID.
      *> ISO 10.5: "A source element is a source unit excluding any
      *>   contained source units" -> OK 10.5; 3.165: "Any source units
      *>   contained within a source unit are part of the containing
      *>   source unit" -> OK 3.165 (a FACTORY or OBJECT paragraph is a
      *>   source unit the class definition contains, Annex D.6.1.1).
      *> 8.3.2.2: "Within a source element, a given user-defined word
      *>   may be used as only one type of user-defined word" -> OK
      *>   8.3.2.2; 8.4.6.1: "each source element may use identical
      *>   user-defined words to name items independent of the use of
      *>   these user-defined words by other source elements" -> OK
      *>   8.4.6.1.
      *> So PB3154G (an object-class-name of the class definition) is a
      *>   data-name of the factory and of the object definition,
      *>   PB3154A (the class's alphabet-name) a factory data-name and
      *>   PB3154H (the CLASS-ID) an object data-name: no source element
      *>   uses one word as two types. Each was COBOLNET2692 before.
      *> The class's configuration still applies to both halves (12.3.4
      *>   GR1 "The entries explicitly or implicitly specified in the
      *>   configuration section of a source unit that contains other
      *>   source units apply to each directly or indirectly contained
      *>   source unit" -> OK 12.3.4): the object method reads the
      *>   class's SYMBOLIC CHARACTERS and CLASS clauses.
      *> Expected values: FSHOW prints the two factory VALUEs; SHOW
      *>   prints the object VALUEs and PB3154S, the character at
      *>   ordinal 67 of the native alphabet (12.3.7.4 GR11), which is
      *>   X"42" = "B"; "123" is in the class PB3154D ("0" THRU "9"),
      *>   so DIGITS prints.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3154H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3154H.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3154H "FSHOW"
           INVOKE PB3154H "NEW" RETURNING OBJ
           INVOKE OBJ "SHOW"
           STOP RUN.
       END PROGRAM PB3154M.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3154G INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS PB3154G.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3154H INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET PB3154A IS NATIVE
           SYMBOLIC CHARACTERS PB3154S IS 67
           CLASS PB3154D IS "0" THRU "9".
       REPOSITORY.
           CLASS BASE
           CLASS PB3154G.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB3154G PIC X VALUE "G".
       01 PB3154A PIC X VALUE "A".
       PROCEDURE DIVISION.
       METHOD-ID. FSHOW.
       PROCEDURE DIVISION.
           DISPLAY "FACTORY " PB3154G " " PB3154A.
       END METHOD FSHOW.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB3154H PIC X VALUE "H".
       01 PB3154G PIC X(3) VALUE "123".
       01 W PIC X.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           MOVE PB3154S TO W
           DISPLAY "OBJECT " PB3154H " " PB3154G " " W
           IF PB3154G IS PB3154D
               DISPLAY "DIGITS"
           END-IF.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB3154H.
