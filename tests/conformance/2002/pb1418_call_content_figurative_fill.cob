      *> kb/Work PB1418 (the CALL arm of the shared argument crossing) -
      *> a figurative constant or ALL literal passed BY CONTENT to a
      *> Format-2 CALL fills the formal parameter.
      *> RULE (14.8.2.3.3 rule 2): for "a program and the NESTED phrase is
      *> specified on the CALL statement", a formal that is not numeric
      *> takes "the same [rules] as for a MOVE statement with the argument
      *> as the sending operand and the corresponding formal parameter as
      *> the receiving operand".
      *> RULE (8.3.3.6.4 GR2): a figurative constant "in association with
      *> a fixed-length data item, literal, or intermediate result" has
      *> its "string of characters ... repeated character by character"
      *> to the item's character positions, then truncated from the right.
      *> cite.py --check 14.8.2.3.3 "the conformance rules are the same as
      *>   for a MOVE statement with the argument as the sending operand"
      *>   -> OK  14.8.2.3.3 2) d)
      *> cite.py --check 8.3.3.6.4 "in association with a fixed-length
      *>   data item, literal, or intermediate result, the string of
      *>   characters is repeated character by character" -> OK
      *>   8.3.3.6.4 2)  (General rules)
      *> DERIVATION: each CALL passes one figurative BY CONTENT to the
      *> PIC X(4) formal L-X, which the callee displays in brackets:
      *>   ALL "*"   -> "****"      ALL "AB" -> "ABAB"
      *>   ZERO      -> "0000"      QUOTE    -> 4 quotation marks
      *> (Before the fix one occurrence crossed: "*   ", "AB  ", "0   ".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1418CM.
       PROCEDURE DIVISION.
           CALL "P1418CS" AS NESTED USING BY CONTENT ALL "*"
           CALL "P1418CS" AS NESTED USING BY CONTENT ALL "AB"
           CALL "P1418CS" AS NESTED USING BY CONTENT ZERO
           CALL "P1418CS" AS NESTED USING BY CONTENT QUOTE
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1418CS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(4).
       PROCEDURE DIVISION USING L-X.
           DISPLAY "X=[" L-X "]"
           GOBACK.
       END PROGRAM P1418CS.
       END PROGRAM P1418CM.
