#!/usr/bin/env python3
"""Generate a lightweight CFamily compile database for Sonar analysis.

The native plugin depends on Unity-side build inputs and astronomy libraries that
are not installed on the GitHub runner. Sonar's CFamily analyzer still needs a
compile database, so this records the intended source files with analysis-only
stub include paths.
"""

from __future__ import annotations

import json
import pathlib
import shlex


ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "ci" / "sonar" / "native_compile_commands.json"
STUBS = ROOT / "ci" / "sonar" / "cfamily-stubs"
NATIVE = ROOT / "native_plugins_cmake"


def quote(path: pathlib.Path | str) -> str:
    return shlex.quote(str(path))


def main() -> None:
    sources = sorted(NATIVE.glob("*.cpp")) + sorted(NATIVE.glob("*.cc"))
    commands = []

    for source in sources:
        output = pathlib.PurePosixPath("/tmp") / f"{source.stem}.o"
        command = " ".join(
            [
                "c++",
                "-std=c++17",
                f"-I{quote(NATIVE)}",
                f"-I{quote(STUBS)}",
                "-D__SONAR_ANALYSIS__",
                "-c",
                quote(source),
                "-o",
                quote(str(output)),
            ]
        )
        commands.append(
            {
                "directory": str(ROOT),
                "command": command,
                "file": str(source),
            }
        )

    OUT.write_text(json.dumps(commands, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {OUT} with {len(commands)} native translation units.")


if __name__ == "__main__":
    main()
