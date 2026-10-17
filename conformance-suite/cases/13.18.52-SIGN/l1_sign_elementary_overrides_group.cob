*>--- conformance-case
*> id: l1_sign_elementary_overrides_group
*> rules: [GR-13.18.52.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.52.4 GR3 — the SIGN clause of an ELEMENTARY numeric item
      *> subordinate to a group that carries its own SIGN clause takes
      *> precedence FOR THAT ELEMENTARY ENTRY, while the group's clause still
      *> governs its siblings.
      *>
      *> THE RULE. §13.18.52.4 GR3: "If a SIGN clause is specified in an
      *> elementary numeric item subordinate to a group item for which a SIGN
      *> clause is specified, the SIGN clause specified in that elementary
      *> entry takes precedence for that elementary entry."
      *> Its complement is GR1: "The SIGN clause specifies the position and
      *> the mode of representation of the operational sign for the numeric
      *> item to which it applies, OR FOR EACH NUMERIC ITEM SUBORDINATE TO THE
      *> GROUP to which it applies." So one group here has to produce TWO
      *> different sign layouts at once, and GR3 decides which entry gets
      *> which.
      *>
      *> WHY THIS GOLDEN USES THE SEPARATE CHARACTER FORM ONLY. §13.18.52.4
      *> GR6 makes both halves of a SEPARATE sign SPEC-DEFINED, so nothing
      *> below depends on an implementor determination:
      *>   GR6 a) "The operational sign is presumed to be the leading (or,
      *>          respectively, trailing) character position of the data item
      *>          to which it applies; this character position is not a digit
      *>          position." -> S9(3) SEPARATE occupies 3 digit positions plus
      *>          ONE more character position = 4 character positions, and
      *>          USAGE DISPLAY is one byte per character position
      *>          (docs/CONFORMANCE.md DOC-A.1-209), so 4 bytes.
      *>   GR6 b) "The operational signs for positive and negative are the
      *>          basic special characters '+' and '-', respectively."
      *>          (The printed standard sets that second character as a
      *>          typographic en dash. The basic special character it names
      *>          is Table 1's "minus sign (hyphen)", §8.1.3.1 — U+002D
      *>          HYPHEN-MINUS — which is what is expected below.)
      *> The unseparate (over-punch) form is deliberately NOT used: §13.18.52.4
      *> GR4/GR5 b) hand its representation to the implementor and that
      *> determination has no docs/CONFORMANCE.md A.1 row yet, so pinning a
      *> character here would pin an undocumented choice.
      *>
      *> DERIVATION OF EVERY EXPECTED CHARACTER.
      *> GNEG carries SIGN IS LEADING SEPARATE at the 01 level.
      *>   GN-A writes its OWN clause, SIGN IS TRAILING SEPARATE. GR3 gives
      *>        that entry to the elementary clause: 3 digits then the sign.
      *>        VALUE -12 in PIC S9(3) is the digit string "012", and GR6 b)
      *>        makes the negative sign '-', so GN-A is [012-].
      *>   GN-B writes no SIGN clause of its own, so GR1 reaches it from the
      *>        group: LEADING SEPARATE, sign then 3 digits = [-012].
      *>   The group image is the concatenation, [012--012], 8 bytes.
      *> GPOS is the same shape at VALUE +12; GR6 b) makes the positive sign
      *>   '+', so [012+] then [+012] = [012++012].
      *>
      *> WHAT EACH LEG DISCRIMINATES — this is not a vacuous pair.
      *>   If the elementary clause were LOST to the group's (GR3 ignored),
      *>   both entries would read LEADING SEPARATE and NEG would be
      *>   [-012-012]. If the group's clause did NOT reach GN-B (GR1 ignored),
      *>   GN-B would fall to the no-clause form of GR4, which occupies 3
      *>   character positions rather than 4, and LEN would read 7 rather
      *>   than 8. Both misreadings change a printed line.
      *> LEN also pins the sizes GR6 a) forces: the group 8, each elementary
      *>   4 — a separate sign character position is NOT a digit position, so
      *>   S9(3) SEPARATE is 4 and not 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SGN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GNEG SIGN IS LEADING SEPARATE.
          05 GN-A PIC S9(3) SIGN IS TRAILING SEPARATE VALUE -12.
          05 GN-B PIC S9(3) VALUE -12.
       01 GPOS SIGN IS LEADING SEPARATE.
          05 GP-A PIC S9(3) SIGN IS TRAILING SEPARATE VALUE +12.
          05 GP-B PIC S9(3) VALUE +12.
       01 XN PIC X(8).
       01 XP PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE GNEG TO XN
           MOVE GPOS TO XP
           DISPLAY "LEN=" FUNCTION BYTE-LENGTH(GNEG)
               " " FUNCTION BYTE-LENGTH(GN-A)
               " " FUNCTION BYTE-LENGTH(GN-B)
           DISPLAY "NEG=[" XN "]"
           DISPLAY "POS=[" XP "]"
           STOP RUN.
