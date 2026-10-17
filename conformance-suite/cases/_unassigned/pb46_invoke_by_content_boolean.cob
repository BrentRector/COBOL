*>--- conformance-case
*> id: pb46_invoke_by_content_boolean
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.23.2 — the INVOKE general format's BY CONTENT branch admits
      *> `arithmetic-expression-1 | boolean-expression-1 | identifier-5 |
      *> literal-2`. The arithmetic operand landed in a previous change
      *> (pb46_invoke_by_content_expression); this is boolean-expression-1, the
      *> operand BY VALUE deliberately does NOT carry, and the one that needed a
      *> value channel of its own (D-B1: a '0'/'1' bit string, never the numeric
      *> or DISPLAY channel).
      *>
      *> THE ROOT CAUSE WAS ONE LAYER LOWER AND FAR WIDER THAN THE OPERAND.
      *> 14.8.2.3.2's identical-description check switched on the formal's
      *> category with arms for object-reference, numeric and alphanumeric, and a
      *> default that answered "formal category {c} is not yet carried across
      *> INVOKE". So a category-BOOLEAN, category-NATIONAL, NUMERIC-EDITED,
      *> POINTER or PROGRAM-POINTER formal parameter was impossible in EVERY
      *> passing mode — BY REFERENCE, BY CONTENT and bare alike. Nothing was
      *> missing but the screen: the three string-imaged categories were already
      *> carried by the marshaling arms. 14.8.2.3.2 states one rule for every
      *> category, and its own lettered exceptions b and c pair a BIT GROUP and a
      *> NATIONAL GROUP with the matching elementary items.
      *>
      *> The crossing itself is 14.8.2.3.3 rule 2d — a non-numeric formal takes
      *> "the same [rules] as for a MOVE statement with the argument as the
      *> sending operand" — so 14.9.25.3 Table 16's BOOLEAN row governs: boolean
      *> and alphanumeric receivers are "Yes", alphabetic / numeric /
      *> numeric-edited are "No" (the negative fixture
      *> pb46-invoke-by-content-boolean-numeric-formal).
      *>
      *> WATCH THE OVER-REACH CONTROLS (13-16). The boolean alternative is gated
      *> by {boolExprAhead()}?, the SHARED condition predicate, whose scan runs to
      *> the statement's period — so in `BY CONTENT N + 1 BY CONTENT B1 B-AND B2`
      *> the FIRST argument reaches the boolean node on the strength of the
      *> SECOND argument's B-AND. The binder reduces a B-operator-free boolean
      *> expression back to its bare operand, so all four earlier channels bind
      *> exactly as they would alone. InvokeContentOperandChannelTests asserts the
      *> over-reach really happens, which is what makes these four evidence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46BOOLCONTENT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB46BOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPB46BOOL.
       01 B1 PIC 1(4) USAGE BIT VALUE B"1100".
       01 B2 PIC 1(4) USAGE BIT VALUE B"1010".
      *> B3 exists ONLY so the write-back case below cannot perturb an operand a
      *> later assertion reads. The first draft flipped B2 in place and every
      *> boolean expression after it silently changed value — still correct, and
      *> unreadable as a golden.
       01 B3 PIC 1(4) USAGE BIT VALUE B"1010".
       01 N  PIC S9(4) VALUE 5.
       01 A  PIC X(2) VALUE "XY".
       01 NA PIC N(4) VALUE N"WXYZ".
       01 ED PIC ZZ9 VALUE 42.
       01 PT USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB46BOOL "NEW" RETURNING O.
      *> 1-4 — the binary and unary boolean operators (8.8.2 rule 7b precedence).
           INVOKE O "TAKEB" USING BY CONTENT B1 B-AND B2.
           INVOKE O "TAKEB" USING BY CONTENT B1 B-OR B2.
           INVOKE O "TAKEB" USING BY CONTENT B1 B-XOR B2.
           INVOKE O "TAKEB" USING BY CONTENT B-NOT B1.
      *> 5-6 — the COBOL-2023 shift operators (8.8.2 rule 8). Gated below 2023 at
      *> THIS site since PB46; before it, a 2023 construct inside a 2002 statement
      *> compiled clean under --std 2002.
           INVOKE O "TAKEB" USING BY CONTENT B1 B-SHIFT-L 1.
           INVOKE O "TAKEB" USING BY CONTENT B1 B-SHIFT-RC 1.
      *> 7 — 8.8.2 rule 10: the value's length is "the number of boolean positions
      *> of the larger ITEM referenced", and a LITERAL is not an item. So the
      *> six-position literal does not widen the result: rule 9 extends B1 on the
      *> right with zeros for the operation, and rule 10 then fixes the value at
      *> B1's four positions.
           INVOKE O "TAKEB" USING BY CONTENT B1 B-AND B"111111".
      *> 8 — boolean literal-2, which had NO channel at all and fell to the
      *> trailing "argument form … not yet carried" diagnostic. 14.9.23.3 SR17
      *> bars only a ZERO-LENGTH literal-2.
           INVOKE O "TAKEB" USING BY CONTENT B"0110".
      *> 9 — a sole boolean identifier under BY CONTENT: the IDENTIFIER arm, which
      *> the conformance screen refused outright before this fix.
           INVOKE O "TAKEB" USING BY CONTENT B1.
      *> 10 — a bare boolean identifier: BY REFERENCE implied (14.9.23.3 SR14), so
      *> the callee's store is visible to the caller afterwards.
           INVOKE O "FLIPB" USING B3.
           DISPLAY "WB=[" B3 "]".
      *> 11 — a boolean expression into an ALPHANUMERIC formal: Table 16 Boolean
      *> row, Alphanumeric column, "Yes"; 14.9.25.4 GR6a — "If the sending item is
      *> of class boolean, its boolean value shall be moved", space-filled.
           INVOKE O "TAKEA" USING BY CONTENT B1 B-XOR B2.
      *> 12 — ANY LENGTH, BOTH picture symbols, asserted as a PAIR. 13.18.2.3 SR1
      *> admits '1' as well as 'N' and 'X', so a category-BOOLEAN formal can carry
      *> ANY LENGTH, and 13.18.2.4 GR1b then makes n the ARGUMENT's length rather
      *> than the one symbol the PICTURE spells (14.8.2.3.3 rule 2c is the
      *> conformance half). The first cut of the emitter arm tested the CATEGORY
      *> before ANY LENGTH and delivered B"1000" as B"1" — silently, and only on
      *> the boolean side, while the alphanumeric twin below was right. The two
      *> are asserted together because that asymmetry is the failure mode.
           INVOKE O "TAKEAL" USING BY CONTENT B1 B-AND B2.
           INVOKE O "TAKEAX" USING BY CONTENT B1 B-AND B2.
      *> 12b — the other two string-imaged sibling categories, plus a pointer, all
      *> blocked by the same default arm.
           INVOKE O "TAKEN" USING NA.
           INVOKE O "TAKEE" USING ED.
           INVOKE O "TAKEP" USING PT.
      *> 13-16 — THE OVER-REACH CONTROLS. A boolean argument later in the SAME
      *> statement must not change how an earlier operand of any other channel
      *> binds: arithmetic expression, identifier, numeric literal, alphanumeric
      *> literal.
           INVOKE O "TWON" USING BY CONTENT N + 1 BY CONTENT B1 B-AND B2.
           INVOKE O "TWON" USING BY CONTENT N BY CONTENT B1 B-AND B2.
           INVOKE O "TWON" USING BY CONTENT 42 BY CONTENT B1 B-AND B2.
           INVOKE O "TWOA" USING BY CONTENT "XY" BY CONTENT B1 B-AND B2.
      *> 17 — BY CONTENT does not write back (the argument has no storage), and
      *> the identifier control proves the caller's item is untouched.
           DISPLAY "B1=[" B1 "]".
           STOP RUN.
       END PROGRAM PB46BOOLCONTENT.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB46BOOL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "B=[" P "]".
       END METHOD TAKEB.
       METHOD-ID. FLIPB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING P.
       M.
           COMPUTE P = B-NOT P.
           DISPLAY "F=[" P "]".
       END METHOD FLIPB.
       METHOD-ID. TAKEA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q PIC X(6).
       PROCEDURE DIVISION USING Q.
       M.
           DISPLAY "A=[" Q "]".
       END METHOD TAKEA.
       METHOD-ID. TAKEAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AL PIC 1 ANY LENGTH.
       PROCEDURE DIVISION USING AL.
       M.
           DISPLAY "AL=[" AL "] LEN=" FUNCTION LENGTH(AL).
       END METHOD TAKEAL.
       METHOD-ID. TAKEAX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 AX PIC X ANY LENGTH.
       PROCEDURE DIVISION USING AX.
       M.
           DISPLAY "AX=[" AX "] LEN=" FUNCTION LENGTH(AX).
       END METHOD TAKEAX.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC N(4).
       PROCEDURE DIVISION USING R.
       M.
           DISPLAY "N=[" R "]".
       END METHOD TAKEN.
       METHOD-ID. TAKEE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 S PIC ZZ9.
       PROCEDURE DIVISION USING S.
       M.
           DISPLAY "E=[" S "]".
       END METHOD TAKEE.
       METHOD-ID. TAKEP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 T USAGE POINTER.
       PROCEDURE DIVISION USING T.
       M.
           IF T = NULL
               DISPLAY "P=[NULL]"
           ELSE
               DISPLAY "P=[SET]"
           END-IF.
       END METHOD TAKEP.
       METHOD-ID. TWON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 U PIC S9(4).
       01 V PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING U V.
       M.
           DISPLAY "2N=[" U "][" V "]".
       END METHOD TWON.
       METHOD-ID. TWOA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 W PIC X(2).
       01 X PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING W X.
       M.
           DISPLAY "2A=[" W "][" X "]".
       END METHOD TWOA.
       END OBJECT.
       END CLASS CPB46BOOL.
