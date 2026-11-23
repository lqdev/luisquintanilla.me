# Path self test

A dead file `Builder.fs` and a root-relative script in a missing folder: `./missing-directory/run.ps1`.

An existing root-relative path `./test-scripts/test-docs-integrity.fsx` and an unknown top folder `other-repo/run.ps1`.

```powershell
./start-server.ps1
./missing-directory/run.ps1
```