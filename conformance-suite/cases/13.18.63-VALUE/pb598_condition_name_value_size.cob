      *>--- conformance-case
      *> id: pb598_condition_name_value_size
      *> rules: [SR-13.18.63.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB598P1.
      *> ISO 13.18.63.3 SR4 - "If the item is of category alphabetic,
      *> alphanumeric, or alphanumeric-edited literals in the VALUE
      *> clause shall be alphanumeric literals.  Alphanumeric literals
      *> in the VALUE clause of AN ELEMENTARY ITEM shall not exceed the
      *> size indicated by AN EXPLICIT PICTURE clause.  Alphanumeric
      *> literals in the VALUE clause of an alphanumeric GROUP ITEM
      *> shall not exceed the size of the group item."
      *>
      *> THE SUBJECT THE SIZE SENTENCES DO NOT NAME (kb/Work PB598).
      *> Sentences 2 and 3 bound two subjects: an elementary item and a
      *> group item.  A Format-3 entry is `88 condition-name-1
      *> value-clause .` (13.16.2) - no PICTURE clause is writable in
      *> it, so no EXPLICIT picture indicates a size - and its subject
      *> is a CONDITION-NAME (13.16.4 GR3; 13.18.63.3 SR33 - "Formats 3
      *> and 5 may be specified only when the level-number of the
      *> subject of the entry is 88"), an entry for which there is "no
      *> true concept of level" (8.5.1.3.2 item 3) and so neither an
      *> elementary item nor a group item (8.5.1.3.1).  13.18.63.4 GR19
      *> gives a condition-name its conditional variable's
      *> characteristics only IMPLICITLY - which is what "explicit"
      *> excludes.  Measured before this fix: every line below was
      *> COBOLNET1740, legal source rejected.
      *>
      *> AND THE STANDARD SAYS WHAT AN OVERSIZE ONE MEANS, which a size
      *> rule would make dead text:
      *>  - 8.8.4.5.3 item 2 - "The rules for comparing a conditional
      *>    variable with a condition-name value are the same as those
      *>    specified for relation conditions", so a longer literal
      *>    simply never compares equal: 8.8.4.2.7 item 2 - "If the
      *>    operands are of unequal length, comparison proceeds as
      *>    though the shorter operand were extended on the right by
      *>    sufficient alphanumeric spaces to make the operands of
      *>    equal length" - a permanently-FALSE condition.
      *>  - 14.9.39.4 GR6 - SET condition-name TO TRUE places the
      *>    literal "according to the rules for the VALUE clause"
      *>    -> 13.18.63.4 GR7 -> 14.6.8.5, "aligned at the leftmost
      *>    character position in the data item with space fill or
      *>    TRUNCATION TO THE RIGHT, as required".
      *>
      *> Every DISPLAY below is derived from those rules, not measured:
      *> T1  XV = "c"; XC is "cd"; "c " vs "cd" differs at position 2
      *>     (space < d) -> FALSE.
      *> T2  SET XC TO TRUE -> 14.6.8.5 truncates "cd" to the right into
      *>     one position -> XV = "c"; and XC is still FALSE.
      *> T3  XV = "c"; XR is the range "aa" THRU "zz" (SR26a: "aa" <
      *>     "zz", conforming).  "c " > "aa" (c > a at position 1) and
      *>     "c " < "zz" (c < z), so 14.7.8's range contains it -> TRUE.
      *> T4  XV = "A"; "A " < "aa" in the native collating sequence
      *>     -> below the range -> FALSE.
      *> T5  YV = "de"; YC is the two-member set "abc" "de"; the second
      *>     member compares equal -> TRUE.  An oversize member does not
      *>     poison the set.
      *> T6  YV = "ab"; "ab" vs "abc" differs at position 3 (space < c)
      *>     and "ab" vs "de" at position 1 -> FALSE.  An oversize
      *>     member does not match by prefix either.
      *> T7  SET YC TO TRUE takes "the value of the first literal"
      *>     (14.9.39.4 GR6) -> "abc" truncated to the right into two
      *>     positions -> YV = "ab", and YC is then FALSE.
      *> T8  The BOUNDARY: a literal of exactly the subject's size is
      *>     unaffected - ZC is "cd" over two positions -> TRUE.
      *>
      *> This program is COBOL-85 source and every rule it exercises is
      *> edition-invariant; ConditionNameValueSizeTests re-compiles the
      *> same shapes at 2002, 2014 and 2023.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XV PIC X.
          88 XC VALUE "cd".
          88 XR VALUE "aa" THRU "zz".
       01 YV PIC X(2).
          88 YC VALUE "abc" "de".
       01 ZV PIC X(2).
          88 ZC VALUE "cd".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "c" TO XV
           IF XC DISPLAY "T1=TRUE" ELSE DISPLAY "T1=FALSE" END-IF
           SET XC TO TRUE
           DISPLAY "T2=[" XV "]"
           IF XC DISPLAY "T2B=TRUE" ELSE DISPLAY "T2B=FALSE" END-IF
           MOVE "c" TO XV
           IF XR DISPLAY "T3=TRUE" ELSE DISPLAY "T3=FALSE" END-IF
           MOVE "A" TO XV
           IF XR DISPLAY "T4=TRUE" ELSE DISPLAY "T4=FALSE" END-IF
           MOVE "de" TO YV
           IF YC DISPLAY "T5=TRUE" ELSE DISPLAY "T5=FALSE" END-IF
           MOVE "ab" TO YV
           IF YC DISPLAY "T6=TRUE" ELSE DISPLAY "T6=FALSE" END-IF
           SET YC TO TRUE
           DISPLAY "T7=[" YV "]"
           IF YC DISPLAY "T7B=TRUE" ELSE DISPLAY "T7B=FALSE" END-IF
           MOVE "cd" TO ZV
           IF ZC DISPLAY "T8=TRUE" ELSE DISPLAY "T8=FALSE" END-IF
           STOP RUN.
