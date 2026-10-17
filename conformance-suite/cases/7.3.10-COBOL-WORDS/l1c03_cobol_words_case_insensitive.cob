      *>--- conformance-case
      *> id: l1c03_cobol_words_case_insensitive
      *> rules: [GR-7.3.10.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.10.4 1) — COBOL-WORDS literals, and the equated and
      *> substituted words, are case-insensitive
      *>
      *> "1) The content of each literal shall be processed as
      *> case-insensitive whenever the COBOL-WORDS directive is applied
      *> within a compilation group. Any use of an equated or
      *> substituted literal shall be case-insensitive when used
      *> syntactically as a COBOL word."
      *>   cite.py: OK  §7.3.10.4 1)  (General rules)   (both sentences)
      *> "2) Each literal ... shall be evaluated as case-insensitive."
      *>   cite.py: OK  §7.3.10.3 2)  (Syntax rules)
      *> The directives must precede the first IDENTIFICATION DIVISION
      *> (§7.3.10.3 SR1), hence their position below this comment.
      *>
      *> Half 1 (literal CONTENT): the literals are written in lower or
      *> mixed case - "display", "Move", "add" - yet each must act on
      *> the reserved word DISPLAY / MOVE / ADD:
      *>   UNDEFINE "add"  -> ADD is no longer reserved, so the
      *>   data-name
      *>                      ADD below is legal (§7.3.10.4 GR3).
      *>   SUBSTITUTE "Move" -> MOVE is no longer reserved, so the
      *>                      data-name MOVE below is legal (GR4).
      *> Half 2 (USE of the equated / substituted word): "show" and
      *> "putval" are used as Show, sHoW, PUTVAL and Putval.
      *>
      *> DERIVATION of each expected line.
      *>   Show "EQ-1"          -> a DISPLAY (GR2 synonym): "EQ-1".
      *>   sHoW "EQ-2"          -> the same synonym, any case: "EQ-2".
      *>   PUTVAL 7 TO ADD      -> the MOVE statement (GR4): ADD = 7.
      *>   Putval "OK" TO MOVE  -> the MOVE statement: MOVE = "OK".
      *>   SHOW ADD " " MOVE    -> "7 OK".
      *> A case-sensitive reading of any literal leaves ADD or MOVE
      *> reserved (the data description is rejected) or leaves Show /
      *> Putval undefined (the statement is rejected).
       >>COBOL-WORDS EQUATE "display" WITH "show"
       >>COBOL-WORDS SUBSTITUTE "Move" BY "putval"
       >>COBOL-WORDS UNDEFINE "add"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ADD   PIC 9  VALUE 0.
       01  move  PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-P.
           Show "EQ-1".
           sHoW "EQ-2".
           PUTVAL 7 TO ADD.
           Putval "OK" TO MOVE.
           SHOW ADD " " MOVE.
           STOP RUN.
