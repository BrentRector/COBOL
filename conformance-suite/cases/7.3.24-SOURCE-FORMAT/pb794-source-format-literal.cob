*>--- conformance-case
*> id: pb794-source-format-literal
*> rules: [FMT-7.3.24.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB794 - a >>SOURCE FORMAT operand written as a LITERAL.
      *>
      *> The sibling of pb794-source-format-unknown, and a different violation: 7.3.3 SR6
      *> composes compiler-instruction out of "compiler-directive words, system-names, and
      *> user-defined words as specified in the syntax of each directive" - a literal is
      *> none of those three, and 7.3.3 SR10 restricts even the directives that DO take one
      *> ("a literal in a compiler directive shall not be specified as a concatenation
      *> expression, a figurative constant, or a floating-point numeric literal"). The
      *> 7.3.24.2 general format writes FIXED and FREE as underlined words, so nothing
      *> quoted can select a reference format.
      *>
      *> It is the spelling GnuCOBOL rejects and this compiler ACCEPTED at battery #62
      *> (run_extensions:5006, AGREE_REJECT -> WE_ACCEPT_THEY_REJECT), which is how the
      *> defect surfaced. Expected: COBOLNET1911.
       >>SOURCE FORMAT "literal"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB794NL.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           STOP RUN.
