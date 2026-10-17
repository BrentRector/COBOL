*>--- conformance-case
*> id: l1-initialize-replacing-category-repeat
*> rules: [SR-14.9.20.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.20.3 SR6 INITIALIZE statement — "The same category shall not be repeated in a
      *> REPLACING phrase."
      *>   python scripts/spec/cite.py --check 14.9.20.3 "The same category shall not be repeated
      *>   in a REPLACING phrase."  ->  OK  §14.9.20.3 6)  (Syntax rules)
      *>
      *> WHY IT REJECTS AT EVERY EDITION. SR6 is a SYNTAX rule, so source that violates it is not a
      *> conforming source program and the compiler owes a diagnostic rather than a behaviour. The
      *> rule text is unchanged across the four editions this compiler targets, and the REPLACING
      *> phrase together with the five category words spellable here is a COBOL-85 construct, so
      *> nothing about this fixture is edition-gated: the reject-at header names all four.
      *>
      *> ⛔ SR6 IS ABOUT THE CATEGORY, NOT ABOUT ADJACENCY, and that is why the repeat here is
      *> SEPARATED. A screen that compared each REPLACING phrase only with its immediate predecessor
      *> would accept this program; a screen that decides on the category the phrase names rejects
      *> it. The two NUMERIC phrases are the violation; the ALPHANUMERIC phrase between them is a
      *> legal, distinct category and must not itself be diagnosed.
      *>
      *> ⛔ EVERYTHING ELSE HERE IS LEGAL, so SR6 is the ONLY ground on which this program can be
      *> rejected — a fixture that also broke a second rule would keep passing for the wrong reason
      *> if the SR6 screen ever stopped firing:
      *>   SR1  identifier-1 (G) is an alphanumeric group item (no GROUP-USAGE clause, not strongly
      *>        typed: §13.18.29.4 GR3), so of class alphanumeric (§8.5.2.1) — admitted outright
      *>        ("Identifier-1 shall be strongly typed or of class alphabetic, alphanumeric,
      *>        boolean, message-tag, national, numeric, object, or pointer.").
      *>   SR3  no DATA-POINTER / FUNCTION-POINTER / MESSAGE-TAG / OBJECT-REFERENCE /
      *>        PROGRAM-POINTER category is named, so identifier-2 is not required and a literal-1
      *>        sender is admissible.
      *>   SR4  "a MOVE statement with identifier-2 or literal-1 as the sending item and an item of
      *>        the specified category as the receiving operand shall be valid" — MOVE 7 TO a
      *>        numeric item and MOVE "ABC" TO an alphanumeric item are both Table-16 'Yes' cells
      *>        (numeric integer -> numeric; alphanumeric -> alphanumeric).
      *>   SR5  no data description entry here carries a RENAMES clause.
      *>
      *> The category words are the standard's own spellings from the §14.9.20.2 category-name
      *> figure ("Every one of the thirteen words is underlined (required word)"), so this fixture
      *> does not depend on any spelling the general format does not define.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INIREP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05 G-N1 PIC 9(3).
           05 G-A1 PIC X(3).
           05 G-N2 PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G
               REPLACING NUMERIC DATA BY 7
                         ALPHANUMERIC DATA BY "ABC"
                         NUMERIC DATA BY 9.
           DISPLAY G-N1 G-A1 G-N2.
           STOP RUN.
