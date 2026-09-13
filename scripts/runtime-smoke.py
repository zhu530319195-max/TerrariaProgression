"""Run the CI-only mod in an isolated disposable dedicated-server world."""
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

repo = Path(__file__).resolve().parents[1]
tml = Path(os.environ["TML_PATH"]).resolve()
artifacts = repo / "artifacts"
artifacts.mkdir(exist_ok=True)
with tempfile.TemporaryDirectory(prefix="tp-runtime-") as temp:
    root = Path(temp)
    mods = root / "Mods"
    mods.mkdir()
    built = Path.home() / ".local/share/Terraria/tModLoader/Mods"
    for name in ("TerrariaProgression", "ProgressionHarness"):
        shutil.copy2(built / (name + ".tmod"), mods)
    (mods / "enabled.json").write_text('["TerrariaProgression", "ProgressionHarness"]')
    command = ["dotnet", str(tml / "tModLoader.dll"), "-server", "-nosteam", "-language", "en-US",
               "-tmlsavedirectory", str(root / "save"), "-modpath", str(mods),
               "-world", str(root / "smoke.wld"), "-autocreate", "1", "-worldname", "Progression CI",
               "-maxplayers", "8", "-port", "7789", "-noupnp"]
    log = artifacts / "runtime-smoke.log"
    with log.open("w") as output:
        process = subprocess.Popen(command, cwd=tml, stdin=subprocess.DEVNULL,
                                   stdout=output, stderr=subprocess.STDOUT,
                                   env={**os.environ, "TP_CI_HARNESS": "1"})
        try:
            code = process.wait(timeout=360)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
            code = 124
    text = log.read_text(errors="replace")
    print(text[-16000:])
    if code != 0 or "TP_RUNTIME_PASS" not in text:
        sys.exit(code or 1)
