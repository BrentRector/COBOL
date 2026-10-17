*>--- conformance-case
*> id: pb390-perform-unknown-procedure-name-1
*> rules: [SR-14.9.28.3-12]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - ISO 14.9.28.3 SR12: "Procedure-name-1 shall be the name of either a paragraph or a
*> section in the same source element as that in which the PERFORM statement is specified." NO-SUCH-PARA
*> is neither, so the reference identifies no resource (8.4.2.1) and 4.2.2 paragraph 2 puts the verdict in
*> the COMPILE-TIME mechanism. Before PB390 the binder DECIDED this rule correctly and then returned
*> BoundUnsupported: the program compiled, an assembly was produced, and the run unit aborted with
*> NotImplementedCobolFeatureException - telling the programmer COBOL.NET was incomplete when the SOURCE
*> is wrong. The statement sits on a path the flow reaches, but the diagnostic must not depend on that.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390PERFSR12.
PROCEDURE DIVISION.
MAIN.
    PERFORM NO-SUCH-PARA.
    STOP RUN.
P1.
    DISPLAY "P1".
