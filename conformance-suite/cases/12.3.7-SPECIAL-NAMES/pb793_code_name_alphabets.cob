*>--- conformance-case
*> id: pb793_code_name_alphabets
*> rules: [SR-12.3.7.3-15, GR-12.3.7.4-7, DOC-A.1-183, DOC-A.1-184]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR15 - "The implementor shall specify the names supported for code-name-1 and
      *> code-name-2 in the ALPHABET clause, if any." Owner decision kb/Work PB793: the code-name-1 set is
      *> ASCII and EBCDIC (the code-name-2 set stays empty). 12.3.7.4 Table 6 gives code-name-1 a Y in BOTH
      *> columns, and 12.3.7.4 GR7 i states what the implementor then owes - "the ordinal number of each
      *> character for use when code-name-1 references a coded character set and the collating position of
      *> each character for use when code-name-1 references a collating sequence ... the correspondence
      *> between characters of the alphanumeric coded character set specified by code-name-1 and the
      *> characters of the native alphanumeric coded character set". This program pins BOTH columns.
      *>
      *> THE DETERMINATIONS (CONFORMANCE.md DOC-A.1-183 / DOC-A.1-184 carry them in full):
      *>   ASCII  = ISO/IEC 646 IRV, 128 characters, ordinal n = native character n-1, native order.
      *>            The same set and sequence 12.3.7.4 GR7 c gives STANDARD-1.
      *>   EBCDIC = IBM CCSID 37, 256 characters; ordinal n is the native character code unit n-1 of that
      *>            page spells; the collating position of a native character IS its CCSID 37 code unit;
      *>            every native character the page does not spell follows, in native relative order.
      *>
      *> EXPECTED VALUES, ALL DERIVED FROM THOSE TWO DETERMINATIONS AND NOTHING ELSE:
      *>   SYMBOLIC Z-EBC IS 241 IN A-EBC - GR11 b: "the value of figurative constant symbolic-character-1
      *>     is the representation of the coded character at ordinal position integer-1 ... in the character
      *>     set referenced by alphabet-name-3". Ordinal 241 = CCSID 37 code unit 240 = X'F0' = DIGIT ZERO,
      *>     whose native correspondence is "0".
      *>   SYMBOLIC Z-ASC IS 66 IN A-ASC - ordinal 66 = native character U+0041 = "A".
      *>   FUNCTION ORD - 15.70.4 r1: "the ordinal position of argument-1 in the current alphanumeric
      *>     program collating sequence" (1-based). Under A-EBC, "A" is at CCSID 37 code unit X'C1' = 193,
      *>     so ORD("A") = 194; "0" is at X'F0' = 240, so ORD("0") = 241; "a" is at X'81' = 129, so 130.
      *>   FUNCTION CHAR - 15.15.4 r1: "the character in the alphanumeric program collating sequence having
      *>     the ordinal position specified by argument-1". CHAR(241) = position 240 = "0".
      *>   The relations - 8.8.4.2.7 compares "with respect to the collating sequence of characters
      *>     specified for the current alphanumeric program collating sequence". EBCDIC puts the lowercase
      *>     letters BELOW the uppercase and both BELOW the digits, which is the reverse of the native
      *>     (ASCII-coincident) order on the second pair: "a" < "A" is TRUE and "A" < "0" is TRUE, where
      *>     natively "a" < "A" is FALSE and "A" < "0" is FALSE. A compiler that ignored the code-name and
      *>     left the sequence native would answer N to both.
      *>   The SORT - 14.9.40.4 GR5 a: the statement's COLLATING SEQUENCE phrase applies to keys of class
      *>     alphabetic and alphanumeric. The released keys are "0", "A" and "a": native order 0 A a,
      *>     EBCDIC order a A 0. THIS PROGRAM DECLARES NO PROGRAM COLLATING SEQUENCE for the SORT leg to
      *>     inherit from - it is a separate source unit - so leg S1 (no phrase, GR5 b, native) and leg S2
      *>     (the phrase) differ only in the phrase, and each is evidence about the other.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB793CODENAME.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS A-EBC.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC
           ALPHABET A-ASC IS ASCII
           SYMBOLIC Z-EBC IS 241 IN A-EBC
           SYMBOLIC Z-ASC IS  66 IN A-ASC
           .
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C1        PIC X.
       01 C2        PIC X.
       01 N1        PIC 999.
       PROCEDURE DIVISION.
           MOVE Z-EBC TO C1
           MOVE Z-ASC TO C2
           DISPLAY "SYMB=[" C1 C2 "]"
           COMPUTE N1 = FUNCTION ORD("A")
           DISPLAY "ORD-A=" N1
           COMPUTE N1 = FUNCTION ORD("0")
           DISPLAY "ORD-0=" N1
           COMPUTE N1 = FUNCTION ORD("a")
           DISPLAY "ORD-a=" N1
           DISPLAY "CHAR241=[" FUNCTION CHAR(241) "]"
           IF "a" < "A" DISPLAY "a-LT-A=Y" ELSE DISPLAY "a-LT-A=N" END-IF
           IF "A" < "0" DISPLAY "A-LT-0=Y" ELSE DISPLAY "A-LT-0=N" END-IF
           CALL "PB793CODENAMESORT"
           STOP RUN.
       END PROGRAM PB793CODENAME.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB793CODENAMESORT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET S-EBC IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb793sort.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR.
          05 SK  PIC X(1).
          05 SP  PIC X(2).
       WORKING-STORAGE SECTION.
       01 DONE-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "S1 NATIVE"
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           DISPLAY "S2 EBCDIC"
           SORT SF ON ASCENDING KEY SK
               COLLATING SEQUENCE IS S-EBC
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           GOBACK.
       FEED.
           MOVE "0" TO SK. MOVE "d0" TO SP.
           RELEASE SR.
           MOVE "A" TO SK. MOVE "uA" TO SP.
           RELEASE SR.
           MOVE "a" TO SK. MOVE "la" TO SP.
           RELEASE SR.
       DRAIN.
           MOVE "N" TO DONE-FLAG.
           PERFORM UNTIL DONE-FLAG = "Y"
               RETURN SF RECORD
                   AT END MOVE "Y" TO DONE-FLAG
                   NOT AT END DISPLAY "  " SP
               END-RETURN
           END-PERFORM.
       END PROGRAM PB793CODENAMESORT.
