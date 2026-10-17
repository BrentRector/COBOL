      *>--- conformance-case
      *> id: l1c02_call_convention_default
      *> rules: [GR-7.3.9.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.9.3 GR1 — with no CALL-CONVENTION directive the
      *> COBOL convention applies: a CALL/CANCEL literal program-name is
      *> a COBOL word, so its letters are case-insensitive.
      *> Rule: "The default for the CALL-CONVENTION directive is
      *> '>>CALL-CONVENTION COBOL'."
      *> cite.py OK lines:
      *>  OK §7.3.9.3 1) The default for the CALL-CONVENTION directive
      *>     is '>>CALL-CONVENTION COBOL'.
      *>  OK §7.3.9.3 2) ... program-names ... specified in subsequent
      *>     ... CALL statements, CANCEL statements ...
      *>  OK §7.3.9.3 2) a) When COBOL is specified, that program-name
      *>     or method-name is treated as a COBOL word that maps to the
      *>     externalized name ...
      *>  OK §8.3.2.2 2) a) When the COBOL call convention is implied
      *>     or COBOL is specified in the CALL-CONVENTION compiler
      *>     directive, the program-name is treated as a COBOL word
      *>  OK §8.1.3.2 3) a) ... COBOL basic letters appearing elsewhere
      *>     within the compilation group are treated in a case-
      *>     insensitive manner.
      *>  OK §14.9.5.4 3) If the program referenced by a successfully
      *>     executed CANCEL statement in a run unit is subsequently
      *>     called in that run unit, that program is in its initial
      *>     state.
      *> No >>CALL-CONVENTION appears in this source, so GR1 puts every
      *> CALL/CANCEL below under the COBOL convention (GR2 a). The
      *> literal is then a COBOL WORD, not a case-sensitive literal
      *> value; its basic letters are case-insensitive (§8.1.3.2 3a),
      *> so "l1c02d", "L1C02D" and "L1c02D" are one word and map to one
      *> externalized name - that of PROGRAM-ID L1C02D - whatever the
      *> implementor's mapping is.
      *> Derivation (L1C02D counts its activations in WORKING-STORAGE,
      *> which persists between CALLs until a CANCEL):
      *>  CALL "l1c02d"   -> found, first activation   -> SUB-COUNT=1
      *>  CALL "L1C02D"   -> same program, state kept  -> SUB-COUNT=2
      *>  CANCEL "l1c02d" -> cancels that same program (GR2 covers
      *>                     CANCEL), so the next CALL finds it in its
      *>                     initial state (§14.9.5.4 GR3)
      *>  CALL "L1c02D"   -> initial state again       -> SUB-COUNT=1
      *>  A case-sensitive (literal) mapping would take ON EXCEPTION on
      *>  the lower/mixed names (NOT-FOUND lines) or leave the CANCEL a
      *>  no-op (SUB-COUNT=3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C02C.
       PROCEDURE DIVISION.
       MAIN.
           CALL "l1c02d"
               ON EXCEPTION DISPLAY "NOT-FOUND-1"
           END-CALL
           CALL "L1C02D"
               ON EXCEPTION DISPLAY "NOT-FOUND-2"
           END-CALL
           CANCEL "l1c02d"
           CALL "L1c02D"
               ON EXCEPTION DISPLAY "NOT-FOUND-3"
           END-CALL
           STOP RUN.
       END PROGRAM L1C02C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C02D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-COUNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO W-COUNT
           DISPLAY "SUB-COUNT=" W-COUNT
           GOBACK.
       END PROGRAM L1C02D.
