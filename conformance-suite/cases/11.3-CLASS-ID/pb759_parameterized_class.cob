*>--- conformance-case
*> id: pb759_parameterized_class
*> rules: [SR-11.3.3-8, GR-11.3.4-5, GR-11.3.4-6, GR-11.6.4-3, GR-11.6.4-4, SR-12.3.8.3-4, SR-12.3.8.3-7, GR-12.3.8.4-1, GR-12.3.8.4-3, GR-12.3.8.4-4, GR-12.3.8.4-5, GR-12.3.8.4-7, GR-12.3.8.4-8, FMT-11.3.2, FMT-11.6.2, FMT-12.3.8.2, DOC-A.1-138, DOC-A.1-201]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB759 -- PARAMETERIZED classes and interfaces, and their EXPANSION.  ISO 11.3.2 and 11.6.2
      *> print `[ USING { parameter-name-1 } ... ]` on the CLASS-ID and INTERFACE-ID paragraphs, and 12.3.8.2
      *> prints the class-specifier / interface-specifier `[ EXPANDS name USING { class | interface } ... ]`.
      *> Before PB759 neither parsed: `CLASS-ID. C USING P.` was COBOL0307 "a period may be missing".
      *>   PB759HOLD  USING ELEM   -- a parameterized class: instance data typed by the formal, a factory
      *>                              counter, and an INVOKE through the formal-typed reference
      *>   PB759LOUD  INHERITS B USING B -- the formal in the INHERITS position (11.3.4 GR6: a parameter-name
      *>                              may be specified wherever an object-class-name is permitted)
      *>   PB759IPUT  USING T      -- a parameterized interface, expanded in TWO source elements under ONE name
      *>   PB759HOLD-LOUD          -- an expansion whose actual parameter is itself an expansion
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  12.3.8.4 GR5: "The class object-class-name-1 is created from the
      *> parameterized class object-class-name-2 by replacing each specification of the formal parameter by
      *> the corresponding actual parameter", and 9.3.12: "An expansion of a parameterized class is treated in
      *> all respects the same as if it were a class that is not a parameterized class".  So PB759HOLD-BOX is
      *> PB759HOLD with ELEM read as PB759BOX: PUT stores B, SHOW-IT invokes PB759BOX's SHOW -> "BOX 042";
      *> PB759HOLD-TXT the same over PB759TXT -> "TXT".  PB759LOUD-BOX INHERITS PB759BOX, so SETV is the
      *> inherited method and SHOW is the OVERRIDE, which displays LOUD and then INVOKEs SUPER -> "LOUD" /
      *> "BOX 007"; PB759HOLD-LOUD holds that object and SHOW-IT dispatches to the same override (9.3.6
      *> runtime-class dispatch) -> "LOUD" / "BOX 007".  9.3.12: each expansion "has its own factory object
      *> and is completely separate from any other instance of the same parameterized class" -> BUMP counts
      *> 1, 2 on PB759HOLD-BOX and starts again at 1 on PB759HOLD-TXT and PB759HOLD-LOUD.  9.3.13: the two
      *> `INTERFACE PB759IPUT-BOX EXPANDS PB759IPUT USING PB759BOX` specifiers (PB759PUTTER's and PB759M's)
      *> name "the same interface instance", so the PB759PUTTER object that IMPLEMENTS the one is a valid
      *> value of P, typed by the other -> "PUTTER PUT" / "BOX 042".
       IDENTIFICATION DIVISION.
       CLASS-ID. PB759BOX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. SETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 NUM PIC 9(3).
       PROCEDURE DIVISION USING NUM.
           MOVE NUM TO V.
       END METHOD SETV.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "BOX " V.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB759BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB759TXT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "TXT".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB759TXT.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB759IPUT USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE T.
       PROCEDURE DIVISION USING X.
       END METHOD PUT.
       END INTERFACE PB759IPUT.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB759HOLD INHERITS FROM BASE USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELEM.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
           ADD 1 TO CNT.
           DISPLAY "COUNT " CNT.
       END METHOD BUMP.
       END FACTORY.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE ELEM.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE ELEM.
       PROCEDURE DIVISION USING X.
           SET ITEM TO X.
       END METHOD PUT.
       METHOD-ID. SHOW-IT.
       PROCEDURE DIVISION.
           INVOKE ITEM "SHOW".
       END METHOD SHOW-IT.
       END OBJECT.
       END CLASS PB759HOLD.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB759LOUD INHERITS B USING B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS B.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW OVERRIDE.
       PROCEDURE DIVISION.
           DISPLAY "LOUD".
           INVOKE SUPER "SHOW".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB759LOUD.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB759PUTTER INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB759BOX
           INTERFACE PB759IPUT
           INTERFACE PB759IPUT-BOX EXPANDS PB759IPUT USING PB759BOX.
       OBJECT.
           IMPLEMENTS PB759IPUT-BOX.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE PB759BOX.
       PROCEDURE DIVISION USING X.
           DISPLAY "PUTTER PUT".
           INVOKE X "SHOW".
       END METHOD PUT.
       END OBJECT.
       END CLASS PB759PUTTER.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB759M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB759BOX
           CLASS PB759TXT
           CLASS PB759HOLD
           CLASS PB759LOUD
           CLASS PB759PUTTER
           INTERFACE PB759IPUT
           CLASS PB759HOLD-BOX EXPANDS PB759HOLD USING PB759BOX
           CLASS PB759HOLD-TXT EXPANDS PB759HOLD USING PB759TXT
           CLASS PB759LOUD-BOX EXPANDS PB759LOUD USING PB759BOX
           CLASS PB759HOLD-LOUD EXPANDS PB759HOLD USING PB759LOUD-BOX
           INTERFACE PB759IPUT-BOX EXPANDS PB759IPUT USING PB759BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B  USAGE OBJECT REFERENCE PB759BOX.
       01 T  USAGE OBJECT REFERENCE PB759TXT.
       01 L  USAGE OBJECT REFERENCE PB759LOUD-BOX.
       01 HB USAGE OBJECT REFERENCE PB759HOLD-BOX.
       01 HT USAGE OBJECT REFERENCE PB759HOLD-TXT.
       01 HL USAGE OBJECT REFERENCE PB759HOLD-LOUD.
       01 P  USAGE OBJECT REFERENCE PB759IPUT-BOX.
       PROCEDURE DIVISION.
           INVOKE PB759BOX "NEW" RETURNING B.
           INVOKE B "SETV" USING 42.
           INVOKE PB759TXT "NEW" RETURNING T.
           INVOKE PB759HOLD-BOX "NEW" RETURNING HB.
           INVOKE HB "PUT" USING B.
           INVOKE HB "SHOW-IT".
           INVOKE PB759HOLD-TXT "NEW" RETURNING HT.
           INVOKE HT "PUT" USING T.
           INVOKE HT "SHOW-IT".
           INVOKE PB759LOUD-BOX "NEW" RETURNING L.
           INVOKE L "SETV" USING 7.
           INVOKE L "SHOW".
           INVOKE PB759HOLD-LOUD "NEW" RETURNING HL.
           INVOKE HL "PUT" USING L.
           INVOKE HL "SHOW-IT".
           INVOKE PB759HOLD-BOX "BUMP".
           INVOKE PB759HOLD-BOX "BUMP".
           INVOKE PB759HOLD-TXT "BUMP".
           INVOKE PB759HOLD-LOUD "BUMP".
           INVOKE PB759PUTTER "NEW" RETURNING P.
           INVOKE P "PUT" USING B.
           STOP RUN.
       END PROGRAM PB759M.
