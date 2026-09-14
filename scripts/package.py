"""Package the player build and complete reproducible source (never bundle the CI mod)."""
import argparse
import hashlib
import re
from pathlib import Path
import subprocess
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument("--mod", type=Path, required=True, help="Path to the compiled TerrariaProgression.tmod")
args = parser.parse_args()
repo = Path(__file__).resolve().parents[1]
output = repo / "artifacts"
output.mkdir(exist_ok=True)
commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repo, text=True).strip()
mod = args.mod.read_bytes()
runtime_log = output / "runtime-smoke.log"
runtime_pass = next((line.strip() for line in runtime_log.read_text(errors="replace").splitlines() if line.startswith("TP_RUNTIME_PASS ")), "Native runtime checks NOT RUN/PASSED") if runtime_log.exists() else "Native runtime checks NOT RUN"
core_log = (output / "core-checks.log").read_text()
core_count = re.search(r"Final P3-Agriculture core checks passed: (\d+)", core_log)
assert core_count and runtime_pass.startswith("TP_RUNTIME_PASS "), "Package requires passing core and native checks"
info = (f"TerrariaProgression P3-Agriculture 0.11.1\nCommit: {commit}\n"
        f"Validation: official compilation and {core_count.group(1)} core checks passed; {runtime_pass}.\n"
        "Target: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8\n"
        f"TerrariaProgression.tmod SHA256: {hashlib.sha256(mod).hexdigest()}\n"
        "P3-Agriculture: 51 numeric entries plus 41 functional entries (including seventeen numeric effects), 92 total. No CI harness in the player package.\n")
with zipfile.ZipFile(output / "TerrariaProgression_P3Agriculture_0.11.1_Test.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    archive.writestr("TerrariaProgression.tmod", mod)
    archive.write(repo / "docs/P3Agriculture_TEST_GUIDE_zh-CN.md", "安装与测试说明.md")
    archive.writestr("BUILD_INFO.txt", info)
with zipfile.ZipFile(output / "TerrariaProgression_P3Agriculture_0.11.1_Source.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    paths = subprocess.check_output(["git", "ls-files", "-z"], cwd=repo).decode().split("\0")
    for path in filter(None, paths):
        archive.writestr(path, subprocess.check_output(["git", "show", f"{commit}:{path}"], cwd=repo))
    archive.writestr("BUILD_INFO.txt", info)
for path in sorted(output.glob("*.zip")):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
    print(path)

