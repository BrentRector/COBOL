*>--- conformance-case
*> id: pb1373-cobol-words-sr3-word-in-no-2023-list
*> rules: [SR-7.3.10.3-3]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.3.10.3 SR3 (cite.py --check 7.3.10.3 "The content of literal-1, literal-3, and literal-4 shall be a
*> reserved word, a context-sensitive word, or an intrinsic function name" -> OK 3)): AUTHOR is in none of 8.9, 8.10 and 8.11
*> (a 1985 reserved word the 2023 list no longer holds), so UNDEFINE "AUTHOR" names no word to undefine. It was accepted
*> because the lexer tokenizes AUTHOR. kb/Work PB1373.
       >>COBOL-WORDS UNDEFINE "AUTHOR"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1373N03.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.
