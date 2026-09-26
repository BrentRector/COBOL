      *> kb/Work PB1350 / PB1351 / PB1354 - COPY REPLACING and REPLACE
      *> match over ISO 7.2.2.5 TEXT-WORDS, compared per 7.2.4.4 8) c)
      *> and 7.2.3.4 9) c); the COPY statement ends at its SEPARATOR
      *> period (7.2.3.4 GR6).
      *> RULES (each run through scripts/spec/cite.py --check):
      *>   7.2.2.5 1) "the colon, the right parenthesis, and the left
      *>     parenthesis characters, in any context except within
      *>     alphanumeric or national literals, are treated as
      *>     separators"                                   -> OK 1)
      *>   7.2.2.5 2) "an alphanumeric, boolean, or national literal
      *>     including the opening and closing delimiters" -> OK 2)
      *>   8.3.5 2) "The COBOL characters comma and semicolon,
      *>     immediately followed by a space, are separators" -> OK 2)
      *>   8.3.5 3) "The COBOL character period, when followed by a
      *>     space, is a separator"                        -> OK 3)
      *>   7.2.4.4 8) c) 3. "Except when used in the non-hexadecimal
      *>     formats of alphanumeric and national literals ... each
      *>     lowercase letter is equivalent to its corresponding
      *>     uppercase letter"                             -> OK 8)
      *>   7.2.4.4 8) c) 4. a. "The two representations of the
      *>     quotation symbol match"; 4. b. "two contiguous occurrences
      *>     of the character used as the quotation symbol ... are
      *>     treated as a single occurrence"                -> OK 8)
      *>   7.2.3.4 6) "... ending with the separator period,
      *>     inclusive"                                    -> OK 6)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [AB]     TBL(1:5): the colon separates, so 5 is a text-word
      *>            and ==5== BY ==2== makes TBL(1:2). A tokenizer
      *>            that keeps "1:5" whole prints [ABCDE].
      *>   [   12]  Z,ZZ9 is ONE text-word (its comma is not followed
      *>            by a space), so ==ZZ9== does not match inside it.
      *>            Splitting at the comma makes Z,999 -> [0,012].
      *>   [ 7.00]  ZZ.ZZ is ONE text-word; ==ZZ== BY ==99== inside it
      *>            would make 99.99 -> [07.00].
      *>   [P"Q]    "P""Q" is ONE literal text-word (content P"Q), so
      *>            =="Q"== cannot match its tail -> not [P"Z].
      *>   [xyz]    "abc" equals pseudo-text "abc" character for
      *>   [ABC]    character; "ABC" does NOT (case is significant in
      *>            literal content) -> a case-folding compare prints
      *>            [xyz] twice.
      *>   [RR]     'QQ' matches =="QQ"== (the two quotation symbols
      *>            match) -> a raw compare leaves [QQ].
      *>   [ZZZ]    COPY ... REPLACING ==VALUE "AAA".== ...: the period
      *>            inside pseudo-text is an operand text-word, not the
      *>            statement's end; the library's VALUE "AAA". is
      *>            replaced. A scan to the first '.' fails to compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1350TW85.
       REPLACE ==5== BY ==2== ==ZZ9== BY ==999== ==ZZ== BY ==99==
               =="Q"== BY =="Z"== =="abc"== BY =="xyz"==
               =="QQ"== BY =="RR"==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL   PIC X(6) VALUE "ABCDEF".
       01 N-ED  PIC Z,ZZ9.
       01 P-ED  PIC ZZ.ZZ.
       01 LIT1  PIC X(3) VALUE "P""Q".
       COPY pb1350cb REPLACING ==VALUE "AAA".== BY ==VALUE "ZZZ".==.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" TBL(1:5) "]"
           MOVE 12 TO N-ED
           DISPLAY "[" N-ED "]"
           MOVE 7 TO P-ED
           DISPLAY "[" P-ED "]"
           DISPLAY "[" LIT1 "]"
           DISPLAY "[" "abc" "]"
           DISPLAY "[" "ABC" "]"
           DISPLAY "[" 'QQ' "]"
           DISPLAY "[" CB-V "]"
           STOP RUN.
