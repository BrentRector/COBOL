      *> reject-at: 2002 2014 2023
      *> kb/Work PB1166 - the LOCALE phrase of the PICTURE clause identity
      *> (§14.8.3.3 "Additionally" 1)), the REJECT half.
      *>   cite.py --check 14.8.3.3 "Locale specifications in the PICTURE
      *>     clauses match if and only if"                          OK 1)
      *>   cite.py --check 8.5.3.1 "where the external identification is
      *>     the external-locale-name or literal value associated with a
      *>     locale-name"                                           OK 2)
      *> The external identification IS the literal value: "en-US" and
      *> "EN_us.UTF-8" select the same locale at run time (CONFORMANCE.md
      *> DOC-A.1-181 normalizes both to en-US) but they are two literal
      *> values, so the two LOCALE phrases do not match and the RETURNING
      *> pair does not conform (COBOLNET0828). Everything else - the SIZE
      *> phrase, the character-string - is the same on both sides.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BNEGLOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       REPOSITORY.
           CLASS W61BNEGLOCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE W61BNEGLOCC.
       01 R PIC +$ZZZZZZ9.99 LOCALE IS US SIZE IS 20.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE W61BNEGLOCC "NEW" RETURNING O
           INVOKE O "GETL" RETURNING R
           STOP RUN.
       END PROGRAM W61BNEGLOC.
       IDENTIFICATION DIVISION.
       CLASS-ID. W61BNEGLOCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE UU IS "EN_us.UTF-8".
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC +$ZZZZZZ9.99 LOCALE IS UU SIZE IS 20.
       PROCEDURE DIVISION RETURNING LR.
           MOVE 1234.5 TO LR.
       END METHOD GETL.
       END OBJECT.
       END CLASS W61BNEGLOCC.
