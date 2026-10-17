      *>--- conformance-case
      *> id: l1_char_national_locale_tie
      *> rules: [DOC-A.1-22]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> DEFECT REPRO for Annex A.1 item 22, NOT a golden to enable
      *> until docs/CONFORMANCE.md row 22 states the determination it
      *> measures.
      *> 15.16.4 r2 has TWO branches and item 22 is the SECOND one: "If
      *> more than one character has the same position in the national
      *> program collating sequence, the character returned is the first
      *> character defined for that character position. If the order of
      *> multiple characters having the same position is UNDEFINED, the
      *> implementor shall define which of those multiple characters is
      *> returned; for a given implementation, collating sequence, and
      *> ordinal position, every invocation of the CHAR-NATIONAL
      *> function shall return the same character."
      *> Item 22's own title is that second sentence verbatim - "which
      *> one of the multiple characters is returned". Sentence ONE is
      *> decided BY THE STANDARD, not by the implementor: under an
      *> ALPHABET ... FOR NATIONAL ... ALSO phrase 12.3.7.4 GR7 k)6
      *> defines the order ("Literal-1 is the first character in the
      *> sequence of multiple characters defined at that ordinal
      *> position"), which is why the existing golden
      *> 2002/l1_char_national_also_positions says in its own header
      *> that it does NOT close item 22. Row 22 nevertheless documents
      *> the ALSO branch, and the code-location it names
      *> (NationalCollation.CharAt) is the ALSO reader, which cannot
      *> reach the undefined-order branch at all.
      *>
      *> THE BRANCH IS REACHABLE, THROUGH A DIFFERENT CLASS. An ALPHABET
      *> ... FOR NATIONAL IS LOCALE sequence is an algorithm, not a
      *> written order: characters the locale collates EQUALLY share an
      *> ordinal position and their relative order is undefined -
      *> exactly r2's second branch. COBOL.NET's answer lives in
      *> LocaleCollation (determination L7: the positions are
      *> materialized once and CHAR takes "the lowest-coded member of a
      *> rank"), documented under the locale facility, never connected
      *> to item 22, and never measured on the NATIONAL arm -
      *> 2002/pb101_alphabet_locale_pcs is the ALPHANUMERIC arm and its
      *> CHAR leg round-trips "Q", which shares its rank with nobody, so
      *> it does not discriminate the tie rule either.
      *> TIE1 position 1 of a locale national sequence is the rank of
      *>      the COMPLETELY IGNORABLE characters, whose members the
      *>      locale does not order; L7 answers the lowest-coded of
      *>      them, U+0000, and FUNCTION ORD of its DISPLAY-OF image
      *>      under the NATIVE alphanumeric sequence is code unit + 1 =
      *>      1. An implementation returning the HIGHEST-coded member of
      *>      the rank, or the first member the collation table happens
      *>      to list, answers something else.
      *> TIE2 r2's third sentence - the same reference returns the same
      *>      character.
      *>
      *> WARNING - THE EXPECTED VALUES ARE DERIVED FROM DETERMINATION
      *> L7, WHICH IS THE IMPLEMENTOR'S OWN DOCUMENTATION BUT NOT ROW
      *> 22'S. If a run disagrees with them, the disagreement is itself
      *> the finding: the determination item 22 is required to document
      *> has no single written answer today.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1CNLOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE
               FOR NATIONAL IS LOC-NAT.
       SPECIAL-NAMES.
           ALPHABET LOC-NAT FOR NATIONAL IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-X   PIC N.
       01 A-X   PIC X.
       01 ORD-R PIC 9(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR-NATIONAL(1) TO N-X
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X
           COMPUTE ORD-R = FUNCTION ORD(A-X)
           DISPLAY "TIE1=" ORD-R
           MOVE FUNCTION CHAR-NATIONAL(1) TO N-X
           MOVE FUNCTION DISPLAY-OF(N-X) TO A-X
           COMPUTE ORD-R = FUNCTION ORD(A-X)
           DISPLAY "TIE2=" ORD-R
           STOP RUN.
