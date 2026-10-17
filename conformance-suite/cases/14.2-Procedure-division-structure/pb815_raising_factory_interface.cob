*>--- conformance-case
*> id: pb815_raising_factory_interface
*> rules: [SR-14.2.2-8, SR-14.2.2-9, GR-14.2.3-12, SR-14.9.18.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB815 + PB814 -- the procedure-division-header RAISING phrase is a TUPLE, and the GOBACK
      *> identifier check compares tuples.  ISO 14.2.1 prints (PDF page 557, folio 527):
      *>     RAISING { exception-name-1 | [ FACTORY OF ] object-class-name-1 | interface-name-1 } ...
      *> with FACTORY underlined and OF plain (an optional word, 5.2.3).  Before PB815 the rule was
      *> `RAISING cobolWord+`, so every FACTORY header below was COBOLNET0901 and every interface header
      *> COBOLNET0858 ("interface names are a later refinement").  Each method writes one alternative:
      *>   WORK-F  RAISING FACTORY OF CE815  + identifier FACTORY OF CE815 -- 14.9.18.3 SR4 a) (same FACTORY)
      *>   WORK-G  RAISING FACTORY CE815     + identifier FACTORY CE815    -- the same, OF omitted on BOTH ends
      *>   WORK-I  RAISING IR815             + identifier IR815            -- SR4 b) (the same interface
      *>           conforms to itself, 9.3.8.2.3) and 14.2.2 SR9
      *>   WORK-A  RAISING CS815             + identifier ACTIVE-CLASS     -- SR4 c): the class containing
      *>           the GOBACK, no FACTORY on either end
      *>   PB815S  PROCEDURE DIVISION RAISING IR815 on a PROGRAM header -- the program arm of the ONE partition
      *> Four methods in ONE class also pin the per-method header: the RAISING state used to be loaded in
      *> the roster loop, so every method was checked against the LAST method's header (WORK-A's CS815).
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  14.9.18.4 GR1 b) 2.: "the object referenced by identifier-1
      *> becomes the current exception object in the activating runtime element" (the INVOKE / CALL
      *> here).  14.9.49.4 GR14 a) runs the class entries first: the factory object of CE815 (SET W-F TO
      *> CE815 -- 14.9.39.3 SR13) is "a factory object ... of object-class-name-1" -> FAC-SEC; SELF in a
      *> CS815 method is an instance of CS815 -> ACT-SEC.  A CR815 instance matches no class entry, so the
      *> USE statements are analyzed again and GR14 b) finds IF-SEC (CR815 IMPLEMENTS IR815).  Each
      *> declarative completes and execution continues after the activating statement.  Expected:
      *> HANDLED CE815 / AFTER WORK-F / HANDLED CE815 / AFTER WORK-G / HANDLED IR815 / AFTER WORK-I /
      *> HANDLED CS815 / AFTER WORK-A / HANDLED IR815 / AFTER PB815S / HANDLED IR815 / AFTER RAISE.
      *> The last pair is RAISE of the same interface-typed reference (14.9.29.4 GR2: EXCEPTION-OBJECT is
      *> set to reference it; GR14 b) selects IF-SEC, after which processing continues) --
      *> the sibling raise site, which emitted an uncompilable conversion for an interface-typed operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB815M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CE815
           CLASS CS815
           CLASS CR815
           INTERFACE IR815.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE OBJECT REFERENCE CS815.
       01 WI USAGE OBJECT REFERENCE IR815.
       PROCEDURE DIVISION.
       DECLARATIVES.
       FAC-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CE815.
       FAC-P.
           DISPLAY "HANDLED CE815".
       ACT-SEC SECTION.
           USE AFTER EXCEPTION OBJECT CS815.
       ACT-P.
           DISPLAY "HANDLED CS815".
       IF-SEC SECTION.
           USE AFTER EXCEPTION OBJECT IR815.
       IF-P.
           DISPLAY "HANDLED IR815".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE CS815 "NEW" RETURNING S.
           INVOKE S "WORK-F".
           DISPLAY "AFTER WORK-F".
           INVOKE S "WORK-G".
           DISPLAY "AFTER WORK-G".
           INVOKE S "WORK-I".
           DISPLAY "AFTER WORK-I".
           INVOKE S "WORK-A".
           DISPLAY "AFTER WORK-A".
           CALL "PB815S".
           DISPLAY "AFTER PB815S".
           INVOKE CR815 "NEW" RETURNING WI.
           RAISE WI.
           DISPLAY "AFTER RAISE".
           STOP RUN.
       END PROGRAM PB815M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB815S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CR815
           INTERFACE IR815.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-I USAGE OBJECT REFERENCE IR815.
       PROCEDURE DIVISION RAISING IR815.
       MAIN-P.
           INVOKE CR815 "NEW" RETURNING W-I.
           GOBACK RAISING W-I.
       END PROGRAM PB815S.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. IR815.
       END INTERFACE IR815.

       IDENTIFICATION DIVISION.
       CLASS-ID. CE815.
       END CLASS CE815.

       IDENTIFICATION DIVISION.
       CLASS-ID. CR815 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE IR815.
       IDENTIFICATION DIVISION.
       OBJECT.
       IMPLEMENTS IR815.
       END OBJECT.
       END CLASS CR815.

       IDENTIFICATION DIVISION.
       CLASS-ID. CS815 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS CE815
           CLASS CR815
           INTERFACE IR815.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. WORK-F.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W-F USAGE OBJECT REFERENCE FACTORY OF CE815.
       PROCEDURE DIVISION RAISING FACTORY OF CE815.
       MAIN.
           SET W-F TO CE815.
           GOBACK RAISING W-F.
       END METHOD WORK-F.
       METHOD-ID. WORK-G.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W-G USAGE OBJECT REFERENCE FACTORY CE815.
       PROCEDURE DIVISION RAISING FACTORY CE815.
       MAIN.
           SET W-G TO CE815.
           GOBACK RAISING W-G.
       END METHOD WORK-G.
       METHOD-ID. WORK-I.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W-I USAGE OBJECT REFERENCE IR815.
       PROCEDURE DIVISION RAISING IR815.
       MAIN.
           INVOKE CR815 "NEW" RETURNING W-I.
           GOBACK RAISING W-I.
       END METHOD WORK-I.
       METHOD-ID. WORK-A.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W-A USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RAISING CS815.
       MAIN.
           SET W-A TO SELF.
           GOBACK RAISING W-A.
       END METHOD WORK-A.
       END OBJECT.
       END CLASS CS815.
