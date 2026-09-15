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
build = dict(line.split(" = ", 1) for line in (repo / "TerrariaProgression/build.txt").read_text().splitlines() if " = " in line)
version = build["version"]
assert re.fullmatch(r"\d+\.\d+\.\d+", version)
mod = args.mod.read_bytes()
runtime_log = output / "runtime-smoke.log"
runtime_pass = next((line.strip() for line in runtime_log.read_text(errors="replace").splitlines() if line.startswith("TP_RUNTIME_PASS ")), "Native runtime checks NOT RUN/PASSED") if runtime_log.exists() else "Native runtime checks NOT RUN"
core_log = (output / "core-checks.log").read_text()
core_count = re.search(r"Final P3-ResourcesLimits core checks passed: (\d+)", core_log)
assert core_count and runtime_pass.startswith("TP_RUNTIME_PASS "), "Package requires passing core and native checks"
info = (f"{build['displayName']} {version}\nInternal name: TerrariaProgression\nCommit: {commit}\n"
        f"Validation: official compilation and {core_count.group(1)} core checks passed; {runtime_pass}.\n"
        "Target: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8\n"
        f"TerrariaProgression.tmod SHA256: {hashlib.sha256(mod).hexdigest()}\n"
        "P3-ResourcesLimits: 52 numeric entries plus 48 functional entries, 100 total. No CI harness in the player package.\n")
with zipfile.ZipFile(output / f"BoundlessPotential_{version}_Release.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    archive.writestr("TerrariaProgression.tmod", mod)
    archive.write(repo / "docs/RELEASE_0.13.1_zh-CN.md", "安装与发布说明.md")
    archive.write(repo / "docs/P3ResourcesLimits_TEST_GUIDE_zh-CN.md", "功能操作说明_0.13.0.md")
    archive.write(repo / "TerrariaProgression/description_workshop.txt", "创意工坊介绍_中英文.txt")
    archive.write(repo / "TerrariaProgression/changelog.txt", "更新说明.txt")
    archive.writestr("BUILD_INFO.txt", info)
with zipfile.ZipFile(output / f"BoundlessPotential_{version}_Source.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    paths = subprocess.check_output(["git", "ls-files", "-z"], cwd=repo).decode().split("\0")
    for path in filter(None, paths):
        archive.writestr(path, subprocess.check_output(["git", "show", f"{commit}:{path}"], cwd=repo))
    archive.writestr("BUILD_INFO.txt", info)
for path in sorted(output.glob("*.zip")):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
    print(path)

