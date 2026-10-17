      *>--- conformance-case
      *> id: l1c34-strong-type-currency-string-differs
      *> rules: [GR-8.5.3.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.3.1 exception 1) — same currency SYMBOL, different
      *>   STRINGS:
      *> the currency symbols do not match.
      *> "Currency symbols match if and only if the corresponding
      *>   currency
      *> strings are the same."
      *> cite.py --check 8.5.3.1 "Currency symbols match if and only
      *>   if the
      *>   corresponding currency strings are the same" -> OK §8.5.3.1
      *>     1)
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>     shall be
      *>   of the same type" -> OK §14.8.2.2 2) (Group items)
      *> cite.py --check 12.3.8.4 "if the externalized name of the
      *>   program
      *>   prototype is the externalized name of a program prototype
      *>     definition
      *>   specified in the same compilation group, the details are
      *>     taken from
      *>   that program prototype definition" -> OK §12.3.8.4 10) b)
      *> The prototype L1C34IS and its definition write AMT PIC $$9
      *>   under
      *> CURRENCY SIGN "#" WITH PICTURE SYMBOL "$"; the caller writes
      *>   PIC $$9
      *> under the default currency string "$". Same symbol, different
      *> strings: by exception 1) the PICTUREs do NOT match, the two TC
      *> declarations are not equivalent, and the strongly-typed
      *>   argument A is
      *> not of the same type as the formal L -- the CALL violates
      *>   §14.8.2.2
      *> and must be rejected (COBOLNET1688,
      *>   call-argument-conformance). The
      *> positive twin 2002/l1c34_strong_type_currency_string_match
      *>   differs
      *> ONLY in the currency clause, so exception 1) is the sole
      *>   reason.
      *> An implementation comparing currency SYMBOLS would accept this.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34IS IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "$".
       DATA DIVISION.
       LINKAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  AMT         PIC $$9.
       01  L TYPE TC.
       PROCEDURE DIVISION USING L.
       END PROGRAM L1C34IS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM L1C34IS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  AMT         PIC $$9.
       01  A TYPE TC.
       PROCEDURE DIVISION.
           CALL L1C34IS USING A.
           DISPLAY "A=[" A "]".
           STOP RUN.
       END PROGRAM L1C34I.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34IS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "$".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  AMT         PIC $$9.
       LINKAGE SECTION.
       01  L TYPE TC.
       PROCEDURE DIVISION USING L.
           MOVE 42 TO AMT OF L.
           DISPLAY "L=[" L "]".
           GOBACK.
       END PROGRAM L1C34IS.
