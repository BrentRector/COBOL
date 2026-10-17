*>--- conformance-case
*> id: pb54_keyword_omitted_figurative_zero_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2 SR2: a function reference written WITHOUT the FUNCTION
      *> keyword is the same reference as one written with it. 8.8.1.1 admits
      *> "the figurative constant ZERO" as an arithmetic operand, and 15.3 admits
      *> an arithmetic expression as an argument. So each pair below is ONE
      *> program written two ways, and the two spellings must agree.
      *>
      *> THEY DID NOT, AND THE KEYWORD-OMITTED HALF RETURNED A WRONG ANSWER
      *> (fix-queue PB54, found sweeping PB50's siblings). The keyword-omitted
      *> form re-parses its captured argument text through FunctionArgFragment,
      *> which did not apply ZeroTokenRewriter - so the bare ZERO kept its
      *> figurative token, ENDED the first argument, and `+ 5` began a second:
      *>
      *>     FUNCTION MIN(ZERO + 5, 2)  =  MIN(5, 2)     = 2   correct
      *>              MIN(ZERO + 5, 2)  =  MIN(0, 5, 2)  = 0   WRONG, and silent
      *>     FUNCTION REM(ZERO + 7, 4)  =  REM(7, 4)     = 3   correct
      *>              REM(ZERO + 7, 4)  -> "takes 2 argument(s); 3 given"
      *>
      *> MIN is the discriminator on purpose: MAX(ZERO + 5, 2) is 5 under BOTH
      *> readings, so a probe built on MAX would have passed while the defect was
      *> live. The arity error on REM is the same cause with a louder symptom.
      *>
      *> Root cause: three fragment re-parsers each hand-assembled lexer, token
      *> stream and parser, and exactly ONE applied the rewriter. They now share
      *> FragmentParse, where the ZERO decision is an explicit argument rather
      *> than an omission.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB54KWOZERO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> 1-2 - MIN: the pair whose two readings give DIFFERENT answers.
           MOVE FUNCTION MIN(ZERO + 5, 2) TO N.
           DISPLAY "1=" N.
           MOVE MIN(ZERO + 5, 2) TO N.
           DISPLAY "2=" N.
      *> 3-4 - REM: fixed arity, so a split argument list is an arity error.
           MOVE FUNCTION REM(ZERO + 7, 4) TO N.
           DISPLAY "3=" N.
           MOVE REM(ZERO + 7, 4) TO N.
           DISPLAY "4=" N.
      *> 5-6 - a BARE figurative argument must KEEP its figurative identity: the
      *> rewriter keys on adjacency to an operator or a plain paren, and a
      *> fragment's text is the content BETWEEN the delimiters, so nothing here
      *> is adjacent. 8.3.3.6.4 GR4 leaves the reading to the BINDER via the
      *> function's own 15.3 argument type - the PB48 rule, which this must not
      *> disturb.
           MOVE FUNCTION MAX(ZERO, 3) TO N.
           DISPLAY "5=" N.
           MOVE MAX(ZERO, 3) TO N.
           DISPLAY "6=" N.
           STOP RUN.
