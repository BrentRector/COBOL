      *>--- conformance-case
      *> id: l1c02_char_locale_tie
      *> rules: [DOC-A.1-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §A.1 item 21 (§15.15.4 r2) — CHAR under an alphanumeric
      *> LOCALE sequence returns the documented member of a tied rank.
      *> The alphanumeric twin of 2002/l1_char_national_locale_tie.
      *> Rule (§15.15.4 r2): "If more than one character has the same
      *> position in the alphanumeric program collating sequence, the
      *> character returned is the first character defined for that
      *> character position. If the order of multiple characters
      *> having the same position is undefined, the implementor shall
      *> define which of those multiple characters is returned; for a
      *> given implementation, collating sequence, and ordinal
      *> position, every invocation of the CHAR function shall return
      *> the same character."
      *> cite.py OK lines:
      *>  OK §A.1 21) CHAR function (which one of the multiple
      *>     characters is returned). This item is required.
      *>  OK §15.15.4 2) If the order of multiple characters having the
      *>     same position is undefined, the implementor shall define
      *>     which of those multiple characters is returned
      *>  OK §15.15.4 2) every invocation of the CHAR function shall
      *>     return the same character.
      *>  OK §12.3.7.4 7) When the LOCALE phrase is specified, the
      *>     collating sequence identified is defined by the locale
      *>     referenced by locale-name-2 when specified, otherwise by
      *>     the locale that is current
      *>  OK §15.70.1 The ORD function returns an integer value that is
      *>     the ordinal position of argument-1 in the program
      *>     collating sequence.
      *> Documented determination (docs/CONFORMANCE.md DOC-A.1-21,
      *> branch 2): "an ALPHABET ... IS LOCALE sequence is an
      *> algorithm, not a written order ... COBOL.NET returns the
      *> LOWEST-CODED member of that rank ... X"00", X"01" and X"1F"
      *> all share ordinal 1 of a locale sequence, and FUNCTION
      *> CHAR(1) is X"00"."
      *> Derivation:
      *>  O00/O01/O1F: the three control characters are ignorable in
      *>    a locale collation, so they share the lowest rank; ORD
      *>    gives that rank's ordinal, 1 (§15.70.1)  -> 001 each. This
      *>    shows the tie (r2's undefined-order branch) is reached.
      *>  C1: CHAR(1) is the documented lowest-coded member X"00";
      *>    its code is read through a BINARY-CHAR UNSIGNED redefinition
      *>    (1 byte, unsigned binary - CONFORMANCE.md DOC-A.1-207), so
      *>    the code value is 0                       -> C1=000.
      *>    Returning X"01" or X"1F" (another member) gives 001 / 031.
      *>  C2: r2's last sentence - the same invocation again returns
      *>    the same character                        -> C2=000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C02G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE IS LOC.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-CH   PIC X.
       01 W-CODE REDEFINES W-CH USAGE BINARY-CHAR UNSIGNED.
       01 W-ORD  PIC 999.
       01 W-OUT  PIC 999.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W-ORD = FUNCTION ORD(X"00")
           DISPLAY "O00=" W-ORD
           COMPUTE W-ORD = FUNCTION ORD(X"01")
           DISPLAY "O01=" W-ORD
           COMPUTE W-ORD = FUNCTION ORD(X"1F")
           DISPLAY "O1F=" W-ORD
           MOVE FUNCTION CHAR(1) TO W-CH
           MOVE W-CODE TO W-OUT
           DISPLAY "C1=" W-OUT
           MOVE "Z" TO W-CH
           MOVE FUNCTION CHAR(1) TO W-CH
           MOVE W-CODE TO W-OUT
           DISPLAY "C2=" W-OUT
           STOP RUN.
