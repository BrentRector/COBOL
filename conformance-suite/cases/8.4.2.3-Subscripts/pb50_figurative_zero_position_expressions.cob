*>--- conformance-case
*> id: pb50_figurative_zero_position_expressions
*> rules: [GR-8.4.2.3.4-1, SR-8.4.3.3.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.1.1: "An arithmetic expression may be ... an identifier
      *> referencing a numeric data item, a numeric literal, THE FIGURATIVE
      *> CONSTANT ZERO (ZEROS, ZEROES) ..."; 8.4.2.3.2 makes a subscript an
      *> arithmetic expression and 8.4.3.3.3 SR4 makes both reference-
      *> modification positions arithmetic expressions. So every line below is
      *> legal source, and each ABORTED AT RUN TIME before fix-queue PB50 with
      *> "a COBOL feature that is not yet implemented was reached".
      *>
      *> TWO CAUSES, NEITHER THE ONE THE QUEUE ENTRY NAMED. The entry blamed a
      *> missing arm in ReferenceResolver.RenderSegment's token switch; PB42 had
      *> already widened that switch's `default:` to route everything
      *> unrenderable to the D18 materializer, so no arm was missing. What was:
      *>   (a) the SUB_IDENTIFIER arm returned null instead of routing to D18 -
      *>       SUBSCRIPT mode has no ZERO token, so the figurative arrives as a
      *>       plain identifier, resolves to no data item, and that early return
      *>       was the ONE place in the renderer deciding a segment was
      *>       unrenderable without asking D18; and
      *>   (b) SubscriptExpressionFragment - the D18 re-parse itself - did not
      *>       apply ZeroTokenRewriter, so even once routed there the bare ZERO
      *>       could not match arithmeticExpression.
      *> Both had to go for either symptom to work.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB50FIGZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X(4) OCCURS 3 TIMES.
       01 T  PIC X(4).
       01 IX PIC 9(2) VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "aaaa" TO E(1).
           MOVE "bbbb" TO E(2).
           MOVE "cccc" TO E(3).
      *> 1-2 - the controls: a literal and a data-name subscript, which always
      *> worked and must keep working (the renderer's fast path).
           MOVE E(1) TO T.
           DISPLAY "1=[" T "]".
           MOVE E(IX + 1) TO T.
           DISPLAY "2=[" T "]".
      *> 3-4 - the figurative in a SUBSCRIPT. ZEROS/ZEROES are the same constant
      *> (8.3.3.6.4), so both spellings are asserted.
           MOVE E(ZERO + 2) TO T.
           DISPLAY "3=[" T "]".
           MOVE E(ZEROS + 3) TO T.
           DISPLAY "4=[" T "]".
      *> 5-7 - the figurative in both REFERENCE-MODIFICATION positions. The
      *> ref-mod symptom reaches the same renderer by a different route and with
      *> a different token type, so it is asserted separately from the subscript.
           MOVE FUNCTION UPPER-CASE("abcdef") (2:3) TO T.
           DISPLAY "5=[" T "]".
           MOVE FUNCTION UPPER-CASE("abcdef") (ZERO + 2:3) TO T.
           DISPLAY "6=[" T "]".
           MOVE FUNCTION UPPER-CASE("abcdef") (2:ZERO + 3) TO T.
           DISPLAY "7=[" T "]".
           STOP RUN.
