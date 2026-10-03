import json, pathlib, sys
here = pathlib.Path(__file__).resolve().parent
for m in sys.argv[1:]:
    p = json.load(open(here / f"t2-{m}.json"))["per"]
    miss = [(pathlib.PurePath(x["report"]).name[:26], x["status"], x["n_leads"]) for x in p if "status" in x and x["status"][0] != x["status"][1]]
    print(m, miss)
