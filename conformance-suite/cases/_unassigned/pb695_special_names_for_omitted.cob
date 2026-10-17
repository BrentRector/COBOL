*>--- conformance-case
*> id: pb695_special_names_for_omitted
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE SPECIAL-NAMES PARAGRAPH'S POST-85 OPTIONAL WORDS, OMITTED (kb/Work PB695).
      *> Companion to conformance-suite/cases/_unassigned/pb695_special_names_optional_words.cob, which covers the
      *> clauses that exist at COBOL-85. This one covers the words the 85 program cannot write: the FOR
      *> that introduces the ALPHANUMERIC/NATIONAL class in the CLASS, SYMBOLIC CHARACTERS and ALPHABET
      *> clauses (a 2002 introduction, `special-names-for-national-2002`), the WITH of the CURRENCY
      *> clause's PICTURE SYMBOL phrase, and the STATUS/IS of the implementor-switch ON and OFF phrases.
      *> ISO 12.3.7.2, read off the printed page (5.2.2 / 5.2.3 - "uppercase words that are not
      *> underlined are called optional words and may be specified at the user's option with no effect
      *> on the semantics of the format", 8.3.2.4.3):
      *>   . folio 290 rules ALPHANUMERIC and NATIONAL and NOT the FOR that introduces them, in the
      *>     CLASS clause; folio 291 and 292 print the same phrase, same way, in the ALPHABET and
      *>     SYMBOLIC CHARACTERS clauses. One printed phrase, and since PB695 ONE grammar rule.
      *>   . folio 290 prints `[ WITH PICTURE SYMBOL literal-8 ]` with rules under PICTURE and SYMBOL only.
      *>   . folio 290's switch rows carry EXACTLY TWO rules on the whole stack - under ON and under OFF.
      *>     STATUS and IS are plain in `ON STATUS IS condition-name-1` and in the OFF row, so all four
      *>     spellings of each phrase are the same statement. `ON STATUS condition-name-1` - STATUS
      *>     written, IS omitted - was unspellable before PB695: the grammar wrote the two alternatives
      *>     `ON STATUS IS x | ON IS? x` and that hand-written power set was missing a member.
      *> DERIVATION of the expected lines:
      *>  . 12.3.7.4 (CLASS clause): a character belongs to class-name-1 when it is one of the
      *>    characters the clause enumerates. "5" is within "0" THRU "9"; "G" is not. The FOR
      *>    ALPHANUMERIC phrase names the class of the literals, which does not change that membership.
      *>  . 12.3.7.4 6) associates each character of the native set with a ONE-BASED ordinal position,
      *>    so the symbolic character at ordinal 66 is the 66th - U+0041 LATIN CAPITAL LETTER A.
      *>  . 12.3.7.4 (CURRENCY SIGN): literal-7 is the currency sign placed in the edited result and
      *>    literal-8 the PICTURE character-string symbol that reserves the position, so PIC ###9.99
      *>    over 123.45 floats the sign to the position left of the first significant digit: `@123.45`.
      *>  . The ALPHABET declaration and the switch entry are not read by the procedure division; that
      *>    they parse and bind at all is what an omitted optional word must not change.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695SNFOR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXDIG ALPHANUMERIC IS "0" THRU "9"
           SYMBOLIC CHARACTERS ALPHANUMERIC SC-ORD66 66
           ALPHABET AL-ALNUM ALPHANUMERIC IS NATIVE
           CURRENCY SIGN IS "@" PICTURE SYMBOL "#"
           SWITCH-2 MNEM-TWO ON STATUS SW-IS-ON OFF STATUS SW-IS-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D5  PIC X VALUE "5".
       01 DG  PIC X VALUE "G".
       01 SC  PIC X.
       01 N   PIC 9(3)V99 VALUE 123.45.
       01 CUR PIC ###9.99.
       PROCEDURE DIVISION.
       MAIN.
           IF D5 IS HEXDIG
               DISPLAY "HEX-5=yes"
           ELSE
               DISPLAY "HEX-5=no"
           END-IF
           IF DG IS HEXDIG
               DISPLAY "HEX-G=yes"
           ELSE
               DISPLAY "HEX-G=no"
           END-IF
           MOVE SC-ORD66 TO SC
           DISPLAY "SC=" SC
           MOVE N TO CUR
           DISPLAY "CUR=" CUR
           DISPLAY "DONE"
           STOP RUN.
