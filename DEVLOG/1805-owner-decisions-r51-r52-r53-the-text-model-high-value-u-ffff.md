## Entry 1805 — 2026-09-29 23:04 PDT — Owner decisions R51 / R52 / R53: the text model, HIGH-VALUE U+FFFF, OPTIONS INITIALIZE fills numeric items

Design first, code after. The owner (a) chose option A for PB1093 — HIGH-VALUE is U+FFFF in the native alphanumeric sequence (R52); (b) asked for the best modern-Unicode design and approved the layered answer (R51): Unicode characters in memory, record BYTES typed per leaf at the medium, an explicit encoding per file (UTF-8 for LINE SEQUENTIAL, one byte per position for fixed-record files, NATIONAL for Unicode inside them); (c) overturned D23 (R53): OPTIONS INITIALIZE fills numeric items too (PB1134).

Finding that shaped the design: HIGH-VALUE = U+FFFF collides with R47 (files are 8859-1; a character above U+00FF is refused on WRITE), so `MOVE HIGH-VALUES TO REC` / WRITE would be refused, and a connector-wide 0xFF<->U+FFFF map cannot tell a text 0xFF from a byte of a binary field (one FD's 01 records REDEFINE the same area). Hence D29 (COBOLNET_FILES_DESIGN): the generated record codec gets a per-leaf MEDIUM image (`AsMedium` / `FromMedium`) distinct from the lossless in-memory group image. R47 is superseded in part (LINE SEQUENTIAL -> UTF-8, PB1760; HIGH-VALUE byte image, PB1759).

Register: decision notes R51, R52, R53; work notes PB1759 (typed medium codec + HIGH-VALUE, absorbs PB1093) and PB1760 (per-file encoding); PB1093 / PB1134 / R47 updated with the decisions. No code changed in this entry — PB1759 lists the unresolved facts to measure before the first edit (47 files reference AsImage/FromImage).

