import os
import subprocess
from pathlib import Path

output_dir = Path(__file__).resolve().parent
input_dir = (output_dir / ".." / "Bannerlord-Mod-Development-Packages" / "Taleworlds").resolve()
os.chdir(input_dir)
ignore_list = ["TaleWorlds.Native.dll"]

for file in os.listdir():
    if not file.endswith(".dll") or file in ignore_list:
        continue
    print("Processing {}...".format(file))
    if subprocess.call(["ilspycmd", "-p", "-o", "{}\\{}".format(output_dir, file[:-4]), file]):
        print("ILspy gave an error while processing {}".format(file))
