      *> ISO/IEC 1989:2023 §13.18.60.4 GR25 + §13.18.60.3 SR19 + §14.9.39.3 SR22 — the RESTRICTED
      *> program-pointer, declared the one way the standard allows and assigned the one way SR22 allows.
      *> kb/Work PB817 (SR-14.9.39.3-22); the declaration half of kb/Work PB609.
      *>
      *> §13.18.60.3 SR19 — "If program-prototype-name-1 is specified, the TYPEDEF clause shall be specified
      *>   for the subject of the entry."  So the restriction is declared as a TYPE DECLARATION (PPT) and
      *>   reached by a TYPE clause; `01 PP USAGE PROGRAM-POINTER TO PBT817.` is itself nonconforming and is
      *>   the negative fixture pb452-program-pointer-to-without-typedef.
      *> §13.18.60.4 GR25 — "If program-prototype-name-1 is specified, this data item is a restricted
      *>   program-pointer. A restricted program-pointer shall contain only the predefined address NULL or the
      *>   address of a program with the same signature as that identified by the specified
      *>   program-prototype-name."
      *> §14.9.39.3 SR22 — "If identifier-7 references a restricted program-pointer, identifier-8 shall be the
      *>   predefined address NULL or shall reference a program-pointer and the program-prototypes associated
      *>   with identifier-7 and identifier-8 shall have the same signature."
      *> §8.4.3.13 GR1/GR2 — SET … TO ENTRY takes the address of the outermost program the literal names.
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   PP-SET     PP1 holds PBT817's address after SET … TO ENTRY, so it is not the predefined NULL.
      *>   PP-SAME    PP2 and PP1 are both restricted to PBT817 — the SAME prototype, hence trivially the same
      *>              signature — so SR22 admits the copy and §8.8.4.2.16 then makes them equal.
      *>   PP-NULL    SR22's first alternative: the predefined address NULL is admissible in a RESTRICTED
      *>              program-pointer whatever its prototype (GR25 says so outright).
      *>   CALLED 0042  The restriction is not decoration: PP2 still addresses PBT817, and a CALL through it
      *>              activates that program, which displays its argument.
      *> kb/Work PB989 - ISO 12.3.8.4 GR10 a): the details come from a program definition specified PREVIOUSLY in the
      *> compilation group; a definition that follows the REPOSITORY entry is not one. The callee below is therefore
      *> given a program prototype definition (11.10.2 Format 2) ahead of every other unit (10.6.2 SR1), which
      *> supplies the details under GR10 b) - the same signature as the definition (10.6.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT817 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PBT817.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB817PP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PBT817.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO PBT817.
       01 PP1 TYPE PPT.
       01 PP2 TYPE PPT.
       01 WS-N PIC 9(4) VALUE 0042.
       PROCEDURE DIVISION.
       MAIN.
           SET PP1 TO ENTRY "PBT817"
           IF PP1 NOT = NULL
               DISPLAY "PP-SET"
           ELSE
               DISPLAY "PP-UNSET"
           END-IF
           SET PP2 TO PP1
           IF PP2 = PP1
               DISPLAY "PP-SAME"
           ELSE
               DISPLAY "PP-DIFF"
           END-IF
           SET PP1 TO NULL
           IF PP1 = NULL
               DISPLAY "PP-NULL"
           ELSE
               DISPLAY "PP-NOTNULL"
           END-IF
           CALL PP2 USING WS-N
           STOP RUN.
       END PROGRAM PB817PP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PBT817.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "CALLED " L-X
           GOBACK.
       END PROGRAM PBT817.
