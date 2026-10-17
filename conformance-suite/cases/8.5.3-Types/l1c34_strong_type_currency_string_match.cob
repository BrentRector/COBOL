      *>--- conformance-case
      *> id: l1c34_strong_type_currency_string_match
      *> rules: [GR-8.5.3.1-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.3.1 exception 1) — currency symbols match by
      *>   currency STRING
      *> "Currency symbols match if and only if the corresponding
      *>   currency
      *> strings are the same."
      *> cite.py --check 8.5.3.1 "Currency symbols match if and only
      *>   if the
      *>   corresponding currency strings are the same" -> OK §8.5.3.1
      *>     1)
      *> cite.py --check 8.5.3.1 "Two type declarations are considered
      *>   equivalent when they have the same type-name" -> OK §8.5.3.1
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
      *> cite.py --check 10.6.2 "the signatures of these two
      *>   compilation units
      *>   shall be the same" -> OK §10.6.2 2)
      *> Separate source units each declare STRONG type TC with one
      *>   item AMT.
      *> The prototype L1C34HS (first, §10.6.2 SR1; its details govern
      *>   the
      *> CALL, §12.3.8.4 GR10 b) and its definition write AMT PIC UU9
      *>   under
      *> CURRENCY SIGN "$" WITH PICTURE SYMBOL "U"; the caller L1C34H
      *>   writes
      *> PIC $$9 under the default currency sign "$". The currency
      *>   SYMBOLS
      *> differ (U vs $) but the currency STRINGS are both "$", so by
      *> exception 1) the PICTUREs match, the declarations are
      *>   equivalent, and
      *> the strongly-typed argument A is of the same type as the
      *>   formal L
      *> (§14.8.2.2): the program is legal. An implementation comparing
      *> currency SYMBOLS would reject it. The negative twin
      *> negative/l1c34-strong-type-currency-string-differs pins the
      *>   other
      *> direction (same symbol, different strings -> no match).
      *> DERIVATION: the callee moves 42 to AMT; floating insertion
      *>   over the 3
      *> positions of UU9 puts the currency string "$" before 42
      *>                                                     -> L=[$42]
      *> A is the same storage (BY REFERENCE)                -> A=[$42]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34HS IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "$" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       LINKAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  AMT         PIC UU9.
       01  L TYPE TC.
       PROCEDURE DIVISION USING L.
       END PROGRAM L1C34HS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34H.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM L1C34HS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  AMT         PIC $$9.
       01  A TYPE TC.
       PROCEDURE DIVISION.
           CALL L1C34HS USING A.
           DISPLAY "A=[" A "]".
           STOP RUN.
       END PROGRAM L1C34H.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34HS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "$" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TC TYPEDEF STRONG.
           05  AMT         PIC UU9.
       LINKAGE SECTION.
       01  L TYPE TC.
       PROCEDURE DIVISION USING L.
           MOVE 42 TO AMT OF L.
           DISPLAY "L=[" L "]".
           GOBACK.
       END PROGRAM L1C34HS.
