      *> reject-at: 2002 2014 2023
      *> kb/Work PB2071 - an UNUSED TYPEDEF template. ISO 13.18.44.3 SR12 (cite.py OK): "The REDEFINES clause shall not
      *> be specified for a data item of class object, message-tag, or pointer or a strongly-typed group item." P, the
      *> subject, is of class pointer. No TYPE clause references the template, which has no storage (ISO 13.18.58.4 GR2,
      *> cite.py OK), but a syntax rule binds the source as written (ISO 4.3), so the template is non-conforming. Before
      *> kb/Work PB2071 ResolveRedefines walked the storage forest only, so this compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W37BPB2071SR12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U IS TYPEDEF STRONG.
          05 X PIC X(8).
          05 P REDEFINES X USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P10".
           STOP RUN.
