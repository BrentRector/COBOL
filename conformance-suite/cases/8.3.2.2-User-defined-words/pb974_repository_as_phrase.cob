*>--- conformance-case
*> id: pb974_repository_as_phrase
*> rules: [SR-8.3.2.2-L3.1, GR-8.4.3.2.4-3, GR-11.6.4-1, SR-12.3.8.3-1,
*>    GR-12.3.8.4-2, GR-12.3.8.4-6, GR-12.3.8.4-9, GR-12.3.8.4-11,
*>    GR-12.3.8.4-15, FMT-12.3.8.2, DOC-A.1-161, DOC-A.1-162,
*>    DOC-A.1-163]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.8.2 (rendered PDF p334-335 / folios 304-305) prints `[ AS literal-n ]`, AS underlined, on
      *> EVERY specifier that names an externalized entity:
      *>     CLASS object-class-name-1 [ AS literal-1 ] [ EXPANDS ... ]
      *>     INTERFACE interface-name-2 [ AS literal-2 ] [ EXPANDS ... ]
      *>     PROGRAM program-prototype-name-1 [ AS literal-3 ]
      *>     PROPERTY property-name-1 [ AS literal-4 ]
      *>     FUNCTION function-prototype-name-1 [ AS literal-5 ]
      *> and the intrinsic-function-specifier as `FUNCTION { intrinsic-function-name-1 } ... INTRINSIC`.
      *> 12.3.8.4 GR2: "If the AS phrase is specified, literal-1, literal-2, literal-3, or literal-5 is the
      *> externalized name by which the class, interface, function, or program, respectively, is known to the
      *> operating environment." So the WORD the program writes is a local name, and it names the definition
      *> whose externalized name is the literal - here each definition is declared with a DIFFERENT word and an
      *> AS literal of its own (11.3.2 CLASS-ID / 11.6.2 INTERFACE-ID / 11.5.2 FUNCTION-ID), so only the
      *> literal can connect the two. kb/Work PB974: every one of these specifiers except PROGRAM refused
      *> `AS` with COBOLNET0901, and `FUNCTION PI E INTRINSIC` died COBOL0307.
      *>
      *>   BOXY  -> the class W974BOX AS "W974-BOX"; its BAL property starts at 100 (13.18.63 VALUE)
      *>   AMOUNT (PROPERTY AMOUNT AS "BAL") -> 12.3.8.3 SR16 a): the property literal-4 of the declared class,
      *>         read then written through the class's BAL property (8.4.3.9)     -> 100, then 555
      *>   TALKY -> the interface W974SPK AS "W974-SPEAKER", which W974BOX's OBJECT IMPLEMENTS (under its own
      *>         local name TALKER AS "W974-SPEAKER"); SPEAK through the interface-typed reference -> SPOKEN
      *>   TWICE -> the function W974FN AS "W974-TWICE" (12.3.8.4 GR11 NOTE 2 - "Literal-5, if specified, is
      *>         the externalized name of the function prototype"), which returns 42  -> 042
      *>   PI E INTRINSIC -> both names usable without FUNCTION (8.4.3.2.3 SR2): 3.14159... + 2.71828... =
      *>         5.85987..., stored in PIC 9 with no ROUNDED phrase, so the fraction is dropped -> 5
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W974FN AS "W974-TWICE".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC 9(3).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 42 TO L-RES
           GOBACK.
       END FUNCTION W974FN.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. W974SPK AS "W974-SPEAKER".
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE W974SPK.

       IDENTIFICATION DIVISION.
       CLASS-ID. W974BOX AS "W974-BOX" INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE TALKER AS "W974-SPEAKER".
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS TALKER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(3) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       PROCEDURE DIVISION.
           DISPLAY "SPOKEN".
       END METHOD SPEAK.
       END OBJECT.
       END CLASS W974BOX.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W974MAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOXY AS "W974-BOX"
           INTERFACE TALKY AS "W974-SPEAKER"
           PROPERTY AMOUNT AS "BAL"
           FUNCTION TWICE AS "W974-TWICE"
           FUNCTION PI E INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE BOXY.
       01 T USAGE OBJECT REFERENCE TALKY.
       01 R PIC 9(3).
       01 N PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE BOXY "NEW" RETURNING B
           DISPLAY AMOUNT OF B
           MOVE 555 TO AMOUNT OF B
           DISPLAY AMOUNT OF B
           INVOKE BOXY "NEW" RETURNING T
           INVOKE T "SPEAK"
           COMPUTE R = FUNCTION TWICE
           DISPLAY R
           COMPUTE N = PI + E
           DISPLAY N
           STOP RUN.
       END PROGRAM W974MAIN.
