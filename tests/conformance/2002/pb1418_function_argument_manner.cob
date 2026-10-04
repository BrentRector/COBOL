      *> kb/Work PB1418 - a user-defined function's arguments cross in
      *> the ISO 8.4.3.2.4 GR5 manner and meet the 14.8.2 conformance
      *> rules that 8.4.3.2.3 SR13 imports.
      *> RULE (8.4.3.2.4 GR5): "a) BY REFERENCE is assumed when the BY
      *> REFERENCE phrase is specified or implied for the corresponding
      *> formal parameter and argument-1 is an identifier that is
      *> permitted as a receiving operand, other than an object
      *> property or object data item. b) BY CONTENT is assumed when
      *> ... argument-1 is a literal, an arithmetic expression, a
      *> boolean expression, an object property, object data item, or
      *> any identifier that is not permitted as a receiving operand."
      *> RULE (13.18.15.3 SR2): "Neither the data item described by the
      *> subject of the entry nor any data item subordinate to the
      *> subject of the entry shall be specified as a receiving data
      *> item." - so a CONSTANT RECORD item crosses BY CONTENT.
      *> RULE (8.4.3.2.3 SR8): "Argument-1 shall be an identifier, a
      *> literal, a boolean expression, or an arithmetic expression."
      *> RULE (15.4): "The evaluation of a function produces a returned
      *> value in a temporary elementary data item."
      *> cite.py --check 8.4.3.2.4 "is an identifier that is permitted
      *>   as a receiving operand, other than an object property or
      *>   object data item" -> OK  8.4.3.2.4 5)  (General rules)
      *> cite.py --check 13.18.15.3 "Neither the data item described by
      *>   the subject of the entry nor any data item subordinate to the
      *>   subject of the entry shall be specified as a receiving data
      *>   item" -> OK  13.18.15.3 2)  (Syntax rules)
      *> cite.py --check 8.4.3.2.3 "Argument-1 shall be an identifier,
      *>   a literal, a boolean expression, or an arithmetic expression"
      *>   -> OK  8.4.3.2.3 8)  (Syntax rules)
      *> cite.py --check 8.3.3.6.4 "in association with a fixed-length
      *>   data item, literal, or intermediate result, the string of
      *>   characters is repeated character by character" -> OK
      *>   8.3.3.6.4 2)  (General rules)
      *> 13.7.3 SR5: "A formal parameter of a function shall not be used as a
      *> receiving operand." (cite.py --check 13.7.3 -> OK 13.7.3 5)), so no
      *> function here stores into its formal. P1418G changes the argument's
      *> storage through the EXTERNAL item WX that the caller describes too,
      *> and returns its formal: only a formal that OCCUPIES the argument's
      *> storage (a BY REFERENCE crossing, 14.2.3 GR8) reads the change.
      *> P1418F returns its formal plus 1; P1418B and P1418U return their
      *> formal unchanged.
      *> DERIVATION of every output line:
      *>  R: WX (41) is a receiving-capable identifier -> BY REFERENCE
      *>     (GR5 a): P1418G adds 1 to WX, which the formal occupies, so it
      *>     returns 42. "R=0042 X=0042".
      *>  C: CX is subordinate to a CONSTANT RECORD, not permitted as a
      *>     receiving operand -> BY CONTENT (GR5 b): the formal holds 5,
      *>     the function returns 5 + 1; the constant keeps 5.
      *>     "C=0006 K=0005".
      *>  E: WX + 1 (= 43) is an arithmetic expression -> BY CONTENT;
      *>     the function returns 44 and WX keeps 42. "E=0044 X=0042".
      *>  B: WB B-AND B"1010" = B"1100" B-AND B"1010" = B"1000", a
      *>     boolean expression -> BY CONTENT into the PIC 1(4) formal,
      *>     moved to the PIC X(4) returning item as its characters
      *>     (Table 16 boolean -> alphanumeric). "B=1000".
      *>  U: FUNCTION UPPER-CASE(WA) is a function-identifier whose
      *>     temporary is alphanumeric "ABCD" (15.4); it crosses BY
      *>     CONTENT with that value. "U=ABCD".
      *>  F: ALL "*" is a figurative constant, a literal (8.3.3.6.3 SR1)
      *>     -> BY CONTENT; "in association with a fixed-length data
      *>     item" it is "repeated character by character" to the item's
      *>     4 character positions (8.3.3.6.4 GR2). "F=****".
      *>  Z: ZERO likewise fills the PIC X(4) formal. "Z=0000".
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1418F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           COMPUTE L-R = L-X + 1
           GOBACK.
       END FUNCTION P1418F.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1418G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC 9(4) EXTERNAL.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           ADD 1 TO WX
           MOVE L-X TO L-R
           GOBACK.
       END FUNCTION P1418G.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1418B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-B PIC 1(4).
       01 L-S PIC X(4).
       PROCEDURE DIVISION USING L-B RETURNING L-S.
           MOVE L-B TO L-S
           GOBACK.
       END FUNCTION P1418B.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1418U.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC X(4).
       01 L-U PIC X(4).
       PROCEDURE DIVISION USING L-A RETURNING L-U.
           MOVE L-A TO L-U
           GOBACK.
       END FUNCTION P1418U.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1418M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P1418F
           FUNCTION P1418G
           FUNCTION P1418B
           FUNCTION P1418U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC 9(4) EXTERNAL.
       01 CR CONSTANT RECORD.
          05 CX PIC 9(4) VALUE 5.
       01 WB PIC 1(4) VALUE B"1100".
       01 WA PIC X(4) VALUE "abcd".
       01 WR PIC 9(4).
       01 WS PIC X(4).
       PROCEDURE DIVISION.
           MOVE 41 TO WX
           COMPUTE WR = FUNCTION P1418G(WX)
           DISPLAY "R=" WR " X=" WX
           COMPUTE WR = FUNCTION P1418F(CX)
           DISPLAY "C=" WR " K=" CX
           COMPUTE WR = FUNCTION P1418F(WX + 1)
           DISPLAY "E=" WR " X=" WX
           MOVE FUNCTION P1418B(WB B-AND B"1010") TO WS
           DISPLAY "B=" WS
           MOVE FUNCTION P1418U(FUNCTION UPPER-CASE(WA)) TO WS
           DISPLAY "U=" WS
           MOVE FUNCTION P1418U(ALL "*") TO WS
           DISPLAY "F=" WS
           MOVE FUNCTION P1418U(ZERO) TO WS
           DISPLAY "Z=" WS
           STOP RUN.
       END PROGRAM P1418M.
