      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB549P AS "PB549X" IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM PB549P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB549M.
      *> kb/Work PB549 at the INTRODUCING EDITION of the construct: §14.9.39.2 Format 9
      *> (program-pointer-assignment) and the §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER that fills it are the
      *> COBOL-2002 program-pointer family.  The identifier's printed general format (PDF p172/folio 142,
      *> RENDERED at 300 dpi) is
      *>       ADDRESS OF PROGRAM { identifier-1 | literal-1 | program-prototype-name-1 }
      *> with ADDRESS and PROGRAM underlined and OF NOT underlined, and the braces are a plain required
      *> choice -- exactly one operand.
      *> ⛔ IT WAS IN THE GRAMMAR AT NO EDITION.  `SET PP TO ADDRESS OF PROGRAM "X"` was
      *> `error COBOL0001: cannot parse construct near 'PROGRAM'`, and the ONLY route into a program-pointer
      *> was `SET PP TO ENTRY "X"` -- the Micro Focus / IBM spelling, whose words appear in no ISO general
      *> format.  So the standard spelling was rejected and the extension was mandatory.
      *> THE FOUR LINES BELOW ARE THE FOUR THINGS THE FORMAT ADMITS:
      *>   1  literal-1                    -- §8.4.3.13.3 SR2, §8.4.3.13.4 GR1 b) "the value of literal-1"
      *>   2  the same with OF OMITTED     -- §8.3.2.4.3: an unstressed reserved word is optional, and the
      *>                                      figure underlines ADDRESS and PROGRAM only
      *>   3  identifier-1                 -- §8.4.3.13.3 SR1 (alphanumeric or national), §8.4.3.13.4 GR1 a)
      *>                                      "the content of the data item referenced by identifier-1"
      *>   4  program-prototype-name-1     -- §8.4.3.13.3 SR3 (declared in the REPOSITORY paragraph)
      *> ⚠ ARM 4 IS THE ONE THAT PINS §8.4.3.13.4 GR2: "For a COBOL program, the address is that of the
      *> outermost program identified by the EXTERNALIZED program-name in its PROGRAM-ID paragraph."  The
      *> prototype is written PB549P and its externalized name is "PB549X", so an implementation that took
      *> the source word instead of the externalized name would not find the program at all.
      *> EXPECTED, derived from those rules and never measured -- four lines, one per CALL through the
      *> pointer, in the order the four arms are written:
      *>   SUB / SUB / SUB / PROTO
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB549P AS "PB549X".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER.
       01  NM PIC X(6) VALUE "PB549S".
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM "PB549S"
           CALL PP
           SET PP TO ADDRESS PROGRAM "PB549S"
           CALL PP
           SET PP TO ADDRESS OF PROGRAM NM
           CALL PP
           SET PP TO ADDRESS OF PROGRAM PB549P
           CALL PP
           STOP RUN.
       END PROGRAM PB549M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB549S.
       PROCEDURE DIVISION.
           DISPLAY "SUB"
           GOBACK.
       END PROGRAM PB549S.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB549P AS "PB549X".
       PROCEDURE DIVISION.
           DISPLAY "PROTO"
           GOBACK.
       END PROGRAM PB549P.
