#!/usr/bin/env python3
"""
Assert a Mach-O binary carries a code signature, without needing `codesign`.

arm64 macOS refuses to EXECUTE an unsigned binary: the kernel SIGKILLs it with no
output (exit 137). `dotnet publish -r osx-arm64` ad-hoc signs the apphost itself,
which is why build/make-macos-app.sh does not call codesign.

This exists so the build can verify that on a LINUX runner, where `codesign` does not
exist. The failure it guards against is invisible in CI and fatal for a player: an
artifact that uploads green and dies instantly on every Mac.

usage: check-macho-signed.py <path-to-macho>
"""
import struct
import sys

LC_CODE_SIGNATURE = 0x1D
MH_MAGIC_64 = 0xFEEDFACF   # little-endian 64-bit
MH_CIGAM_64 = 0xCFFAEDFE   # byte-swapped
FAT_MAGIC = 0xCAFEBABE


def slices(data):
    """Yield (offset, endian) for each Mach-O in the file, fat or thin."""
    magic = struct.unpack_from(">I", data, 0)[0]
    if magic in (FAT_MAGIC, 0xBEBAFECA):
        count = struct.unpack_from(">I", data, 4)[0]
        for i in range(count):
            # fat_arch: cputype, cpusubtype, offset, size, align
            off = struct.unpack_from(">I", data, 8 + i * 20 + 8)[0]
            yield off, arch_endian(data, off)
    else:
        yield 0, arch_endian(data, 0)


def arch_endian(data, off):
    magic = struct.unpack_from("<I", data, off)[0]
    if magic == MH_MAGIC_64:
        return "<"
    if magic == MH_CIGAM_64:
        return ">"
    raise SystemExit(f"not a 64-bit Mach-O at offset {off}: magic={magic:#x}")


def has_signature(data, off, endian):
    ncmds = struct.unpack_from(endian + "I", data, off + 16)[0]
    pos = off + 32  # past mach_header_64
    for _ in range(ncmds):
        cmd, cmdsize = struct.unpack_from(endian + "II", data, pos)
        if cmd == LC_CODE_SIGNATURE:
            return True
        if cmdsize == 0:
            break
        pos += cmdsize
    return False


def main():
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)

    path = sys.argv[1]
    with open(path, "rb") as f:
        data = f.read()

    for off, endian in slices(data):
        if not has_signature(data, off, endian):
            print(f"FAIL {path}: no LC_CODE_SIGNATURE (slice at {off}). "
                  f"arm64 macOS will SIGKILL this.")
            return 1

    print(f"OK {path} carries a code signature")
    return 0


if __name__ == "__main__":
    sys.exit(main())
