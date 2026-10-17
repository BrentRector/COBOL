      *>--- conformance-case
      *> id: pb3154_half_data_hides_class_symbolic
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3154 review R1-1 - a class definition's SYMBOLIC
      *>   CHARACTERS name is HIDDEN, inside its object definition and
      *>   its methods, by a nearer declaration of the same spelling.
      *> ISO 8.4.6.1: "a source element's reference to such a name, even
      *>   when it is a different type of user-defined word, is to the
      *>   item which that source element describes" -> OK 8.4.6.1.
      *> ISO 8.4.6.2.2: "A constant-name, data-name, record-name,
      *>   file-name, or type-name declared in a source element for an
      *>   object definition, whether factory or instance, is a global
      *>   name" -> OK 8.4.6.2.2; so a method references it, and by
      *>   8.4.6.2.1 3) a) "If the name is declared in source element B,
      *>   the item in source element B is the referenced item" -> OK
      *>   8.4.6.2.1 3) a), a method's own item is nearer still.
      *> NESTING: class PB3154SC declares the symbolic-characters
      *>   S1 (67) and S2 (68). Its OBJECT declares a data-name S1
      *>   ("Z") and a constant-name K ("K"); its method SHOWA declares
      *>   the local data-names S2 ("Q") and K ("L"); SHOWB declares
      *>   none. Its FACTORY declares a data-name S2 ("F").
      *> EXPECTED OUTPUT, DERIVED:
      *>   F BF   factory method SHOWF: S1 the class's symbolic (67 =
      *>          X"42" = "B"; the OBJECT's S1 is another element's),
      *>          S2 the factory's own data-name.
      *>   A ZQL  SHOWA: S1 is the object's data-name (nearer than the
      *>          class's symbolic), S2 and K the method's own items.
      *>   B ZCK  SHOWB: S1 the object's data-name, S2 the class's
      *>          symbolic (ordinal 68 = X"43" = "C", 12.3.7.4 GR11),
      *>          K the object's constant - SHOWA's names are its own.
      *> Before the fix: "F BC", "A BCK" and "B BCK".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154SP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3154SC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3154SC.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3154SC "SHOWF"
           INVOKE PB3154SC "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3154SP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3154SC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S1 S2 ARE 67 68.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2 PIC X VALUE "F".
       01 FW PIC X(2).
       PROCEDURE DIVISION.
       METHOD-ID. SHOWF.
       PROCEDURE DIVISION.
           STRING S1 S2 DELIMITED BY SIZE INTO FW
           DISPLAY "F " FW.
       END METHOD SHOWF.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X VALUE "Z".
       01 K CONSTANT AS "K".
       01 W PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 S2 PIC X VALUE "Q".
       01 K PIC X VALUE "L".
       PROCEDURE DIVISION.
           STRING S1 S2 K DELIMITED BY SIZE INTO W
           DISPLAY "A " W.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       PROCEDURE DIVISION.
           STRING S1 S2 K DELIMITED BY SIZE INTO W
           DISPLAY "B " W.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3154SC.
