*>--- conformance-case
*> id: oo-multi-base-inherits
*> rules: [FMT-11.3.2]
*> source-format: free
*> portable: unverified
*>---
*> COBOL.NET v1 single-inheritance restriction (SSOT §18 #18; A.4.10): CLASS-ID ... INHERITS FROM two or
*> more base classes (legal ISO §11.3.2 syntax) is rejected LOUDLY — never silently compiled against only
*> the first base (the R9 silent-miscompile this negative pins).
CLASS-ID. MULTIBASE INHERITS FROM BASEA BASEB.
END CLASS MULTIBASE.
CLASS-ID. BASEA.
END CLASS BASEA.
CLASS-ID. BASEB.
END CLASS BASEB.
