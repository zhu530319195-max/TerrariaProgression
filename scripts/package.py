"""Package the player build and complete reproducible source (never bundle the CI mod)."""
import argparse
import hashlib
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
info = (f"TerrariaProgression P1-A 0.2.0\nCommit: {commit}\n"
        "Target: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8\n"
        f"TerrariaProgression.tmod SHA256: {hashlib.sha256(mod).hexdigest()}\n"
        "P1-A: 21 numeric talents and talent panel. No CI harness in the player package.\n")
with zipfile.ZipFile(output / "TerrariaProgression_P1_0.2.0_Test.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    archive.writestr("TerrariaProgression.tmod", mod)
    archive.write(repo / "docs/P1_TEST_GUIDE_zh-CN.md", "安装与测试说明.md")
    archive.writestr("BUILD_INFO.txt", info)
with zipfile.ZipFile(output / "TerrariaProgression_P1_0.2.0_Source.zip", "w", zipfile.ZIP_DEFLATED) as archive:
    paths = subprocess.check_output(["git", "ls-files", "-z"], cwd=repo).decode().split("\0")
    for path in filter(None, paths):
        archive.write(repo / path, path)
    archive.writestr("BUILD_INFO.txt", info)
for path in sorted(output.glob("*.zip")):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
    print(path)
