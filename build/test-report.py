#!/usr/bin/env python3
"""
Turn the output of a run of the fish game's tests into a report.

    python3 build/test-report.py --log tests.log --list tests.list --exit-code 0 --out test-results

    --log        what the game printed during the run (`Fade.MonoGame --no-banner --output Detailed`)
    --list       what `Fade.MonoGame --list-tests` printed, so that a test that never ran can be named
    --exit-code  what the run exited with
    --out        the folder that the report goes in

It writes, into --out:

    summary.md   the counts, every test that failed with what it said, and every test that never ran
    junit.xml    the same, for anything that reads JUnit
    results.tsv  one line for every test: its name and how it went

and it prints summary.md, so that a workflow can put it in the summary of the job.

Why the list matters: a test that crashes the program, or that hangs until the run is timed out,
ends the whole run with no summary and no name. The tests run in the order that they are listed,
so the first one that was listed and never reported is the one to look at.

It exits with 0 when every listed test passed, and with 1 otherwise.
"""
import argparse
import os
import re
import sys
from xml.sax.saxutils import escape, quoteattr

STATUS_LINE = re.compile(r"^(passed|failed|skipped)\s+(\S+)")
LISTED_LINE = re.compile(r"^\s+([A-Za-z_][A-Za-z0-9_]*)\s*$")
FAILURE_LINES_KEPT = 40


def read_lines(path):
    if not path or not os.path.exists(path):
        return []
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        return [line.rstrip("\r\n") for line in handle]


def read_listed(path):
    """The names that --list-tests printed, in order. They are the indented lines."""
    names = []
    for line in read_lines(path):
        if line.startswith("Test discovery summary"):
            break
        match = LISTED_LINE.match(line)
        if match:
            names.append(match.group(1))
    return names


def read_results(lines):
    """How every test that reported went, in order, and what a failed one said after its line."""
    order = []
    status = {}
    said = {}
    current_failure = None
    for line in lines:
        match = STATUS_LINE.match(line)
        if match:
            how, name = match.group(1), match.group(2)
            if name not in status:
                order.append(name)
            status[name] = how
            current_failure = name if how == "failed" else None
            if current_failure:
                said[name] = []
            continue
        if line.startswith("Test run summary"):
            current_failure = None
        if current_failure and len(said[current_failure]) < FAILURE_LINES_KEPT:
            # the game's own chatter is not what the test said
            if not line.startswith("[fade]") and line.strip():
                said[current_failure].append(line)
    return order, status, said


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--log", required=True)
    parser.add_argument("--list", default="")
    parser.add_argument("--exit-code", type=int, default=0)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()

    lines = read_lines(args.log)
    listed = read_listed(args.list)
    order, status, said = read_results(lines)

    # with no list, the tests that reported are all that is known
    names = listed if listed else order
    for name in order:
        if name not in names:
            names.append(name)

    passed = [n for n in names if status.get(n) == "passed"]
    failed = [n for n in names if status.get(n) == "failed"]
    skipped = [n for n in names if status.get(n) == "skipped"]
    never_ran = [n for n in names if n not in status]
    finished = any(line.startswith("Test run summary") for line in lines)

    good = len(names) > 0 and not failed and not never_ran and args.exit_code == 0

    md = []
    md.append("## Fish game tests: " + ("passed" if good else "FAILED"))
    md.append("")
    md.append("| | |")
    md.append("| --- | --- |")
    md.append(f"| tests | {len(names)} |")
    md.append(f"| passed | {len(passed)} |")
    md.append(f"| failed | {len(failed)} |")
    md.append(f"| skipped | {len(skipped)} |")
    md.append(f"| never ran | {len(never_ran)} |")
    md.append(f"| exit code of the run | {args.exit_code} |")
    md.append("")
    if not names:
        md.append("No test reported, and there was no list of tests. The run did not get as far as the first test.")
        md.append("")
    if not finished:
        md.append("The run ended without its summary, so it crashed or was timed out.")
        if never_ran:
            md.append(f"The first test that never ran is `{never_ran[0]}`, which is the one to look at first.")
        md.append("")
    if failed:
        md.append("### Failed")
        md.append("")
        for name in failed:
            md.append(f"- `{name}`")
            if said.get(name):
                md.append("")
                md.append("  ```")
                md.extend("  " + line for line in said[name])
                md.append("  ```")
        md.append("")
    if never_ran:
        md.append("### Never ran")
        md.append("")
        shown = never_ran[:50]
        md.extend(f"- `{name}`" for name in shown)
        if len(never_ran) > len(shown):
            md.append(f"- and {len(never_ran) - len(shown)} more. see results.tsv.")
        md.append("")
    summary = "\n".join(md)

    os.makedirs(args.out, exist_ok=True)
    with open(os.path.join(args.out, "summary.md"), "w", encoding="utf-8") as handle:
        handle.write(summary + "\n")

    with open(os.path.join(args.out, "results.tsv"), "w", encoding="utf-8") as handle:
        for name in names:
            handle.write(f"{name}\t{status.get(name, 'never ran')}\n")

    with open(os.path.join(args.out, "junit.xml"), "w", encoding="utf-8") as handle:
        handle.write('<?xml version="1.0" encoding="UTF-8"?>\n')
        handle.write(
            f'<testsuite name="fish" tests="{len(names)}" failures="{len(failed)}" '
            f'errors="{len(never_ran)}" skipped="{len(skipped)}">\n'
        )
        for name in names:
            how = status.get(name)
            handle.write(f"  <testcase classname=\"fish\" name={quoteattr(name)}")
            if how == "passed":
                handle.write("/>\n")
            elif how == "failed":
                text = escape("\n".join(said.get(name, [])))
                handle.write(f">\n    <failure message=\"failed\">{text}</failure>\n  </testcase>\n")
            elif how == "skipped":
                handle.write(">\n    <skipped/>\n  </testcase>\n")
            else:
                handle.write(">\n    <error message=\"never ran\"/>\n  </testcase>\n")
        handle.write("</testsuite>\n")

    print(summary)
    return 0 if good else 1


if __name__ == "__main__":
    sys.exit(main())
