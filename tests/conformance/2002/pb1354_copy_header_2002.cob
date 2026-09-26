      *> kb/Work PB1354 / PB1350 - the COPY statement is PARSED in its
      *> 7.2.3.2 general-format order over 7.2.2.5 text-words, and
      *> prefixed literals and concatenation operands are whole
      *> text-words under REPLACE.
      *> RULES (each run through scripts/spec/cite.py --check):
      *>   7.2.3.4 8) "Each matched occurrence of pseudo-text-1 ... in
      *>     the library text is replaced by the corresponding
      *>     pseudo-text-2"                                -> OK 8)
      *>   7.2.3.3 5) "Literal-1 and literal-2 shall be alphanumeric
      *>     literals"                                     -> OK SR5
      *>   7.2.2.5 2) "an alphanumeric, boolean, or national literal
      *>     including the opening and closing delimiters" -> OK 2)
      *>   8.3.5 5) opening delimiters "the two contiguous characters
      *>     B", B', N", N', X", and X'"                   -> OK 5)
      *>   7.2.4.4 8) c) 2. "Each operand and operator of a
      *>     concatenation expression is a separate text-word" -> OK 8)
      *> The general format prints [ SUPPRESS PRINTING ] BEFORE the
      *> optional REPLACING phrase (PRINTING is not underlined).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [HELLO]  COPY "pb1354cb" SUPPRESS PRINTING REPLACING ...:
      *>            literal-1 names the library text by its value, and
      *>            the REPLACING phrase after SUPPRESS is applied, so
      *>            BB-Y is declared. A positional header scan drops
      *>            the phrase and BB-Y is undefined.
      *>   [HELLO]  the same with bare SUPPRESS (CC-Z).
      *>   [A]      X"41" is ONE text-word; =="41"== cannot match its
      *>            tail (a split prefix prints [B]).
      *>   [ABZZ]   "AB" & "CD": the operand "CD" is a text-word of its
      *>            own and =="CD"== replaces it.
      *>   [AC]     "A" & X"42": the prefixed operand X"42" is ONE
      *>            text-word, matched whole by ==X"42"==.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1354HDR02.
       REPLACE =="41"== BY =="42"== =="CD"== BY =="ZZ"==
               ==X"42"== BY ==X"43"==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY "pb1354cb" SUPPRESS PRINTING
            REPLACING ==AA-X== BY ==BB-Y==.
       COPY pb1354cb SUPPRESS REPLACING ==AA-X== BY ==CC-Z==.
       01 H1 PIC X VALUE X"41".
       01 C1 PIC X(4) VALUE "AB" & "CD".
       01 C2 PIC X(2) VALUE "A" & X"42".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" BB-Y "]"
           DISPLAY "[" CC-Z "]"
           DISPLAY "[" H1 "]"
           DISPLAY "[" C1 "]"
           DISPLAY "[" C2 "]"
           STOP RUN.
