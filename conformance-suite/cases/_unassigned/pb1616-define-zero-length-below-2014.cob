      *>--- conformance-case
      *> id: pb1616-define-zero-length-below-2014
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1616 - the zero-length literal is a COBOL-2014
      *>   introduction (VCR row 7.30, kb/Work PB895), and a literal
      *>   in a compiler-directive operand is the same token as one in
      *>   the program text, lexed apart from the compilation group. The
      *>   unit's literal screen never saw it, so this compiled clean
      *>   at COBOL-2002. ISO 8.3.3.1: "If the opening and closing
      *>   delimiters are contiguous, the length of the literal is
      *>   zero, and it is known as a zero-length literal."
      *>   cite.py --check 8.3.3.1 "the length of the literal is zero,
      *>     and it is known as a zero-length literal" -> OK
      *> At 85 the >>DEFINE directive is itself unintroduced; both are
      *>   COBOLNET0900. The 2014 twin is conformance:2014/
      *>   pb1616_define_zero_length_literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1616NZL.
       PROCEDURE DIVISION.
       MAIN-P.
       >>DEFINE ZL AS ""
           STOP RUN.
