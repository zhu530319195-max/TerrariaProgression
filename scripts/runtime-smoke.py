"""Run the CI-only mod in an isolated disposable dedicated-server world."""
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import threading

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
        process = subprocess.Popen(command, cwd=tml, stdin=subprocess.PIPE,
                                   stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                                   env={**os.environ, "TP_CI_HARNESS": "1"})
        def read_output():
            for line in process.stdout:
                output.write(line)
                output.flush()
                if "Server started" in line:
                    process.stdin.write("tpci\n")
                    process.stdin.flush()
        pump = threading.Thread(target=read_output, daemon=True)
        pump.start()
        try:
            code = process.wait(timeout=360)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
            code = 124
        pump.join(timeout=5)
    text = log.read_text(errors="replace")
    print("\n".join(line for line in text.splitlines() if "% - " not in line)[-16000:])
    if code != 0 or "TP_RUNTIME_PASS" not in text:
        sys.exit(code or 1)
