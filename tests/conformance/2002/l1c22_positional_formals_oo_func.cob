      *> ISO §14.2.3 GR2/GR5 — function reference, INVOKE USING, inline
      *> invocation and implicit SET property arguments bind by
      *> position.
      *> RULE (14.2.3 GR2): "The arguments passed from the activating
      *> element are: ... the arguments specified in the USING phrase of
      *> an INVOKE statement -- the arguments specified in an inline
      *> invocation of a method -- the arguments specified in a function
      *> reference -- the argument defined by the rules of an
      *> object-property for invocation of an implicit SET property
      *> method. The correspondence between the arguments and the
      *> formal parameters is established on a positional basis."
      *> RULE (14.2.3 GR5): "Data-name-1 is a formal parameter for the
      *> function, method, or program."
      *> cite.py --check 14.2.3 "the arguments specified in an inline
      *>   invocation of a method" -> OK  §14.2.3 2)  (General rules)
      *> cite.py --check 14.2.3 "the argument defined by the rules of
      *>   an object-property for invocation of an implicit SET property
      *>   method" -> OK  §14.2.3 2)  (General rules)
      *> cite.py --check 14.2.3 "The correspondence between the
      *>   arguments and the formal parameters is established on a
      *>   positional basis" -> OK  §14.2.3 2)  (General rules)
      *> cite.py --check 14.2.3 "Data-name-1 is a formal parameter for
      *>   the function, method, or program" -> OK  §14.2.3 5)
      *> (The CALL arm is pinned at 85 by
      *> l1c22_call_positional_formals.)
      *> The caller names its items X and Y; the function and the method
      *> name their formals X and Y too; every activation passes the
      *> caller's items in the OPPOSITE order (Y, X). A name-based
      *> binding would print AAABBB on the first three lines.
      *> DERIVATION of every output line (caller X = "AAA", Y = "BBB"):
      *>  F: FUNCTION L1C22F(Y X): formal 1 X gets "BBB", formal 2 Y
      *>     gets "AAA"; the function strings X then Y into its
      *>     returning item: "F=BBBAAA".
      *>  I: INVOKE L1C22M "CAT" USING Y X: method formal X = "BBB",
      *>     Y = "AAA", returns X then Y: "I=BBBAAA".
      *>  N: inline invocation L1C22M :: "CAT" (Y X): same binding:
      *>     "N=BBBAAA".
      *>  P: MOVE Y TO P OF L1C22M invokes the implicit SET property
      *>     method with the caller's Y ("BBB") as its one argument,
      *>     which becomes the value of factory item P: "P=BBB".
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1C22F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(3).
       01 Y PIC X(3).
       01 R PIC X(6).
       PROCEDURE DIVISION USING X Y RETURNING R.
       P-MAIN.
           STRING X Y DELIMITED BY SIZE INTO R
           GOBACK.
       END FUNCTION L1C22F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22J.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION L1C22F
           CLASS L1C22M
           PROPERTY P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "AAA".
       01 Y PIC X(3) VALUE "BBB".
       01 W-R PIC X(6) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION L1C22F(Y X) TO W-R
           DISPLAY "F=" W-R
           MOVE SPACES TO W-R
           INVOKE L1C22M "CAT" USING Y X RETURNING W-R
           DISPLAY "I=" W-R
           MOVE SPACES TO W-R
           MOVE L1C22M :: "CAT" (Y X) TO W-R
           DISPLAY "N=" W-R
           MOVE Y TO P OF L1C22M
           DISPLAY "P=" P OF L1C22M
           STOP RUN.
       END PROGRAM L1C22J.
       IDENTIFICATION DIVISION.
       CLASS-ID. L1C22M.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X(3) VALUE "---" PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. CAT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(3).
       01 Y PIC X(3).
       01 R PIC X(6).
       PROCEDURE DIVISION USING X Y RETURNING R.
       P-MAIN.
           STRING X Y DELIMITED BY SIZE INTO R.
       END METHOD CAT.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C22M.
