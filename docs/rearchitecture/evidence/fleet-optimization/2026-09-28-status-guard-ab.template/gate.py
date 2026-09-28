"""The project gate: tests, then the house rules. Prints one verdict line: GATE: GREEN or GATE: RED (<reasons>)."""
import ast, pathlib, subprocess, sys

reasons = []
t = subprocess.run([sys.executable, "-m", "unittest", "-q"], capture_output=True, text=True)
if t.returncode != 0:
    reasons.append("tests fail")
for mod in ("inventory.py", "pricing.py", "report.py"):
    tree = ast.parse(pathlib.Path(mod).read_text(encoding="utf-8"))
    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.ClassDef)) and not node.name.startswith("_"):
            if not ast.get_docstring(node):
                reasons.append(f"{mod}: {node.name} has no docstring")
        if isinstance(node, ast.Call) and getattr(node.func, "id", "") == "print":
            reasons.append(f"{mod}: print() in library code (line {node.lineno})")
log = pathlib.Path("CHANGELOG.md").read_text(encoding="utf-8").lower()
for mod in ("inventory", "pricing", "report"):
    if mod not in log.split("## unreleased", 1)[-1]:
        reasons.append(f"CHANGELOG: no Unreleased entry naming {mod}")
print("GATE: GREEN" if not reasons else "GATE: RED (" + "; ".join(reasons) + ")")
sys.exit(0 if not reasons else 1)
