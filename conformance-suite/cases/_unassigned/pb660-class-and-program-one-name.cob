*>--- conformance-case
*> id: pb660-class-and-program-one-name
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 8.3.2.2, FIRST sentence - an outermost PROGRAM definition and an object-CLASS definition in
*> one compilation group externalize the same name: "Within a run unit, all instances of a given name that is
*> externalized to the operating environment shall identify the same kind of entity or item." Its list item 1
*> puts BOTH in the externalized population ("program-names of outermost programs, object-class-names,
*> function-prototype-names, interface-names, method-names, program-prototype-names, property-names, and
*> user-function-names").
*> THE SIBLING SWEEP FIXTURE (kb/Work PB660): before that landing every definition namespace policed only
*> ITSELF - class-vs-class by COBOLNET0820 and interface-vs-class by COBOLNET0840 under 8.4.6.4's own
*> uniqueness sentences, function-vs-function by COBOLNET1508, program-vs-program by nothing at all - so no
*> check ever compared a definition of one kind against a definition of another and this compiled clean.
*> COBOLNET2213 is now the ONE check over 8.3.2.2's whole list. It does NOT restate 8.4.6.4: a class/class or
*> class/interface pair sharing the declared WORD is left to COBOLNET0820/0840, which own that sentence.
*> reject-at begins at 2002 because a CLASS-ID definition is a COBOL-2002 introduction.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660XM.
PROCEDURE DIVISION.
MAIN-M.
    DISPLAY "OK".
    GOBACK.
END PROGRAM PB660XM.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB660BOTH.
PROCEDURE DIVISION.
MAIN-P.
    GOBACK.
END PROGRAM PB660BOTH.
IDENTIFICATION DIVISION.
CLASS-ID. PB660BOTH.
END CLASS PB660BOTH.
