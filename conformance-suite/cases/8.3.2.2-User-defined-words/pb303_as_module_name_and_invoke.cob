*>--- conformance-case
*> id: pb303_as_module_name_and_invoke
*> rules: [SR-8.3.2.2-L3.6, GR-8.3.2.2-L4.2, GR-11.7.4-1, GR-11.10.4-1, FMT-11.7.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the AS externalized-name phrase, the half only COBOL-2023 can
       *> observe: WHICH of the two names each surface reports.
       *>
       *> 1. FUNCTION MODULE-NAME(CURRENT) reports the DECLARED name PB303MO, not the AS
       *>    literal.  ISO 15.65.4 r4 leaves the FORM implementor-defined - "The
       *>    implementor may return the name as specified in the program-id, function-id,
       *>    or method-id paragraph.  They may also return the name as specified in the AS
       *>    clause of the Identification Division if used" - and docs/CONFORMANCE.md
       *>    DOC-A.1-135 documents the program-id form.  WS-NAME is PIC X(10) and
       *>    "PB303MO" is seven characters, so the MOVE space-fills three to the right
       *>    (ISO 14.9.24.4 GR3, alphanumeric receiving item) - the brackets make that
       *>    fill visible, and a compiler returning the eight-character "PB303MOX" would
       *>    show one fewer space.
       *> 2. INVOKE WS-OBJ "PB303MDX" reaches METHOD-ID PB303MD AS "PB303MDX".  ISO
       *>    14.9.23.2 gives INVOKE no WORD form for the method at all - only literal-1 or
       *>    identifier-2 - and 14.9.23.4 GR2 a) resolves that literal "as described in
       *>    8.3.2.2, User-defined words", i.e. against the externalized name.  The method
       *>    is therefore reachable ONLY under its AS literal, exactly as CALL is.
       *> 3. CLASS-ID PB303CL AS "PB303CLX" carries the phrase too (ISO 11.3.2), while the
       *>    object-class-name stays the word every reference uses (8.4.6.4) - the
       *>    REPOSITORY entry and the OBJECT REFERENCE clause below both name PB303CL.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB303CL AS "PB303CLX" INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. PB303MD AS "PB303MDX".
       PROCEDURE DIVISION.
       MD-P.
           DISPLAY "METHOD-VIA-AS-LITERAL".
           GOBACK.
       END METHOD PB303MD.
       END OBJECT.
       END CLASS PB303CL.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303MO AS "PB303MOX".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB303CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-OBJ USAGE OBJECT REFERENCE PB303CL.
       01  WS-NAME PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION MODULE-NAME(CURRENT) TO WS-NAME.
           DISPLAY "CUR=[" WS-NAME "]".
           INVOKE PB303CL "NEW" RETURNING WS-OBJ.
           INVOKE WS-OBJ "PB303MDX".
           STOP RUN.
       END PROGRAM PB303MO.
