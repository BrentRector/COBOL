*>--- conformance-case
*> id: pb46_invoke_by_content_expression
*> rules: [GR-14.9.23.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.23.2 — the INVOKE general format's BY CONTENT branch admits
      *> `arithmetic-expression-1 | boolean-expression-1 | identifier-5 |
      *> literal-2`. Read off the RENDERED figure, not the prose.
      *>
      *> `invokeArgument` admitted only `(dataReference | literal)`, and the
      *> failure was NOT a parse error (fix-queue PB46). `BY CONTENT N + 1`
      *> matched `N` as a dataReference and started a SECOND argument at `+ 1`,
      *> so the statement reported COBOLNET0828 "2 USING argument(s) for 1
      *> formal parameter(s)" — an arity diagnostic about a rule the program
      *> does not violate, pointing the reader at the method signature. A silent
      *> mis-parse is worse than a rejection.
      *>
      *> 14.8.2.3.3 2) a) governs the crossing: "the conformance rules are
      *> the same as for a COMPUTE statement", so the formal must be
      *> category numeric. The non-conforming pairing is the negative fixture
      *> pb46-invoke-by-content-expression-alphanumeric-formal.
      *>
      *> ⚠ THE CONTROLS BELOW ARE THE POINT AS MUCH AS THE FIX IS. A first cut
      *> wrote the alternation as `(nonNumericLiteral | arithmeticExpression)`,
      *> and because `arithmeticExpression` SUBSUMES `dataReference`, a bare
      *> `BY CONTENT A` silently began matching the expression arm — losing the
      *> 14.9.23.3 SR9/SR10 object-data rules, the 14.8.2.3.2 conformance check
      *> and the ref-mod handling — while `BY CONTENT "XY"` stopped binding at
      *> all. Both are asserted here so that regression cannot return.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46BYCONTENT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB46X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPB46X.
       01 N PIC S9(4) VALUE 5.
       01 A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB46X "NEW" RETURNING O.
      *> 1-2 — arithmetic-expression-1, the operand the format admits and the
      *> grammar refused. 5+1 = 6; 5*2-3 = 7.
           INVOKE O "TAKEN" USING BY CONTENT N + 1.
           INVOKE O "TAKEN" USING BY CONTENT N * 2 - 3.
      *> 3 — a sole identifier under BY CONTENT still takes the IDENTIFIER path.
           INVOKE O "TAKEN" USING BY CONTENT N.
      *> 4 — a numeric literal under BY CONTENT.
           INVOKE O "TAKEN" USING BY CONTENT 42.
      *> 5-6 — the alphanumeric controls: a literal and an identifier.
           INVOKE O "TAKEA" USING BY CONTENT "XY".
           INVOKE O "TAKEA" USING BY CONTENT A.
      *> 7-8 — the other passing modes are untouched.
           INVOKE O "TAKEN" USING BY REFERENCE N.
           INVOKE O "TAKEN" USING N.
           STOP RUN.
       END PROGRAM PB46BYCONTENT.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB46X INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC S9(4).
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "N=[" P "]".
       END METHOD TAKEN.
       METHOD-ID. TAKEA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Q PIC X(4).
       PROCEDURE DIVISION USING Q.
       M.
           DISPLAY "A=[" Q "]".
       END METHOD TAKEA.
       END OBJECT.
       END CLASS CPB46X.
