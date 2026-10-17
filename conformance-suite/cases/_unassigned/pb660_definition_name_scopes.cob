      *>--- conformance-case
      *> id: pb660_definition_name_scopes
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.2.2 + §8.4.6.3 — the POSITIVE control for kb/Work
      *> PB660's uniqueness check: three shapes the standard permits,
      *> each of which a coarser check would have rejected.
      *> (1) §8.4.6.3's scope is ONE OUTERMOST PROGRAM — "The names
      *> assigned to programs that are contained directly or indirectly
      *> within the same outermost program shall be unique within that
      *> outermost program" — so PB660PA and PB660PB may EACH contain a
      *> program called PB660IN. Rule 1 of the same clause keeps the two
      *> apart at every reference: "If the program-name is that of a
      *> program that does not possess the common attribute and that is
      *> directly contained within another program, that program-name
      *> may be referenced only by statements included in that
      *> containing program".
      *> (2) A contained program's name is NOT externalized — §8.3.2.2
      *> externalizes "program-names of outermost programs" — so a
      *> containee never collides with an outermost definition and the
      *> group-wide check must not see it at all.
      *> (3) What §8.3.2.2 makes unique is the EXTERNALIZED name, and
      *> "For any externalized user-defined words for which the AS
      *> phrase is specified, the content of the literal specified in
      *> that AS phrase is a name that is externalized": two outermost
      *> programs may therefore share the declared WORD PB660PW while
      *> externalizing PB660X1 and PB660X2, and both are callable.
      *> Expected output is derived from those rules, not measured: each
      *> AS NESTED call reaches its OWN container's containee, and each
      *> literal CALL reaches the definition externalizing that literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660PM.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB660PA"
           CALL "PB660PB"
           CALL "PB660X1"
           CALL "PB660X2"
           STOP RUN.
       END PROGRAM PB660PM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660PA.
       PROCEDURE DIVISION.
       A-MAIN.
           CALL "PB660IN" AS NESTED.
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660IN.
       PROCEDURE DIVISION.
       A-IN.
           DISPLAY "A-INNER".
           GOBACK.
       END PROGRAM PB660IN.
       END PROGRAM PB660PA.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660PB.
       PROCEDURE DIVISION.
       B-MAIN.
           CALL "PB660IN" AS NESTED.
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660IN.
       PROCEDURE DIVISION.
       B-IN.
           DISPLAY "B-INNER".
           GOBACK.
       END PROGRAM PB660IN.
       END PROGRAM PB660PB.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660PW AS "PB660X1".
       PROCEDURE DIVISION.
       W1-MAIN.
           DISPLAY "W-ONE".
           GOBACK.
       END PROGRAM PB660PW.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB660PW AS "PB660X2".
       PROCEDURE DIVISION.
       W2-MAIN.
           DISPLAY "W-TWO".
           GOBACK.
       END PROGRAM PB660PW.
