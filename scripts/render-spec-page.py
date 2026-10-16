#!/usr/bin/env python3
"""Render one or more pages of the canonical ISO COBOL PDF to PNG for visual inspection.

The markdown `<a id="page-N">` anchors map 1:1 onto PDF pages, so N is passed directly.
Usage:  python3 scripts/render-spec-page.py <page> [<page> ...] [--dpi 300] [--out DIR]

Each page is written to DIR/spec_p<N>.png and its path printed. Without --out, DIR is a NEW directory of this run's own
under the machine-wide temp directory (`spec-pages-XXXXXXXX`): the temp directory is one place for every agent on the
host, so a fixed name there would let two renders of one page overwrite each other (kb/Work PB2922). Read the path
this prints, never a guessed one.
"""
import sys, os, glob, tempfile, fitz

# The licensed PDF lives in the PRIVATE submodule (the repo layout changed 2026-07-27; `specs/` is now the
# public Markdown transcription). Globbed rather than named, so a re-download with a different order suffix
# still resolves.
PDF = next(iter(sorted(glob.glob(os.path.join(
    os.path.dirname(__file__), '..', 'specs-private', '*COBOL*.pdf')))), None)

def main(argv):
    pages, dpi, out = [], 300, None
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--dpi':   dpi = int(argv[i+1]); i += 2; continue
        if a == '--out':   out = argv[i+1];      i += 2; continue
        pages.append(int(a)); i += 1
    if PDF is None or not os.path.exists(PDF):
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in "
                 "a PRIVATE submodule: git submodule update --init specs-private")
    if out is None:
        out = tempfile.mkdtemp(prefix='spec-pages-')
    else:
        os.makedirs(out, exist_ok=True)
    doc = fitz.open(PDF)
    for p in pages:
        if not (1 <= p <= doc.page_count):
            print(f'!! page {p} out of range (1..{doc.page_count})'); continue
        pix = doc[p-1].get_pixmap(dpi=dpi)          # anchor page-N == PDF page N
        f = os.path.join(out, f'spec_p{p}.png')
        pix.save(f)
        print(f'{f}  ({pix.width}x{pix.height} @ {dpi}dpi)')

if __name__ == '__main__':
    main(sys.argv[1:])
