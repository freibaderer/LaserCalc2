#!/usr/bin/env python3
"""Dependency-free preflight checks for the LaserCalc source tree.
This is not a C# compiler and does not replace a Windows/.NET build.
"""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []
xml_files = list(ROOT.rglob("*.xaml")) + list(ROOT.rglob("*.csproj")) + [ROOT / "Directory.Build.props"]
for path in xml_files:
    try:
        ET.parse(path)
    except ET.ParseError as exc:
        errors.append(f"XML fehlerhaft: {path.relative_to(ROOT)}: {exc}")

handler_count = 0
for xaml in ROOT.rglob("*.xaml"):
    codebehind = xaml.with_suffix(".xaml.cs")
    if not codebehind.exists():
        continue
    source = codebehind.read_text(encoding="utf-8")
    methods = set(re.findall(
        r"\b(?:private|public|protected|internal)\s+(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+(\w+)\s*\(",
        source,
    ))
    tree = ET.parse(xaml)
    for element in tree.iter():
        for key, value in element.attrib.items():
            event = key.rsplit("}", 1)[-1]
            if event in {"Click", "Loaded", "TextChanged", "SelectionChanged", "Drop", "DragOver", "Checked", "Unchecked", "KeyDown", "MouseDoubleClick"}:
                handler_count += 1
                if value not in methods:
                    errors.append(f"XAML-Handler fehlt: {xaml.name} -> {value} ({codebehind.name})")

project_refs = 0
for project in ROOT.rglob("*.csproj"):
    source = project.read_text(encoding="utf-8")
    for reference in re.findall(r'<ProjectReference Include="([^"]+)"', source):
        project_refs += 1
        target = (project.parent / reference.replace("\\", "/")).resolve()
        if not target.exists():
            errors.append(f"Projektverweis fehlt: {project.name} -> {reference}")


def find_invalid_regular_string_newlines(source: str) -> list[int]:
    """Find literal newlines inside normal C# strings (not raw triple-quoted strings)."""
    positions: list[int] = []
    i, n = 0, len(source)
    while i < n:
        if source.startswith('"""', i):
            end = source.find('"""', i + 3)
            i = n if end < 0 else end + 3
            continue
        if source[i] in ('"', "'"):
            quote = source[i]
            i += 1
            while i < n:
                if source[i] == "\\":
                    i += 2
                    continue
                if source[i] == quote:
                    i += 1
                    break
                if source[i] in "\r\n":
                    if quote == '"':
                        positions.append(i)
                    break
                i += 1
            continue
        i += 1
    return positions

# Small lexical pass to spot accidental brace/paren corruption. It skips comments and strings.
def strip_literals(source: str) -> str:
    output: list[str] = []
    i, n = 0, len(source)
    while i < n:
        if source.startswith("//", i):
            end = source.find("\n", i)
            i = n if end < 0 else end
            output.append("\n")
        elif source.startswith("/*", i):
            end = source.find("*/", i + 2)
            i = n if end < 0 else end + 2
        elif source.startswith('"""', i):
            end = source.find('"""', i + 3)
            i = n if end < 0 else end + 3
        elif source[i] in ('"', "'"):
            quote = source[i]
            i += 1
            while i < n:
                if source[i] == "\\":
                    i += 2
                elif source[i] == quote:
                    i += 1
                    break
                else:
                    i += 1
        else:
            output.append(source[i])
            i += 1
    return "".join(output)

cs_files = list(ROOT.rglob("*.cs"))
for path in cs_files:
    source_text = path.read_text(encoding="utf-8")
    for position in find_invalid_regular_string_newlines(source_text):
        errors.append(f"Zeilenumbruch in regulärem C#-String: {path.relative_to(ROOT)} bei Position {position}")
    text = strip_literals(source_text)
    stack: list[str] = []
    pairs = {")": "(", "]": "[", "}": "{"}
    for position, char in enumerate(text):
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack[-1] != pairs[char]:
                errors.append(f"Klammerfehler: {path.relative_to(ROOT)} bei Position {position}")
                break
            stack.pop()
    if stack:
        errors.append(f"Offene Klammer: {path.relative_to(ROOT)} ({stack[-1]})")

verification = ROOT / "src/LaserCalc.Verification/Program.cs"
checks = re.findall(r'^Check\("([^"]+)"', verification.read_text(encoding="utf-8"), re.M)
if len(checks) != 38:
    errors.append(f"Erwartet 38 Verifikationsfälle, gefunden: {len(checks)}")

customer_files = [p for p in ROOT.rglob("*") if p.is_file() and p.suffix.lower() in {".dxf", ".geo"}]
if customer_files:
    errors.append("Kunden-DXF/GEO-Dateien dürfen nicht im Quellcodepaket liegen: " + ", ".join(str(p) for p in customer_files))

print(f"XML-Dateien: {len(xml_files)} geprüft")
print(f"XAML-Ereignisse: {handler_count} geprüft")
print(f"C#-Dateien: {len(cs_files)} auf Klammerstruktur geprüft")
print(f"Projektverweise: {project_refs} geprüft")
print(f"Verifikationsfälle: {len(checks)} im C#-Prüfprojekt")
print(f"Kunden-DXF/GEO-Dateien im Quellbaum: {len(customer_files)}")
if errors:
    print("\nFEHLER:")
    for error in errors:
        print("-", error)
    sys.exit(1)
print("\nStrukturprüfung erfolgreich. Hinweis: kein C#-Compile oder WPF-Laufzeittest.")
