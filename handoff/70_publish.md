# Natane Toon Shader v1.4.6 Publish Checklist

## Goal

Prepare and publish `com.natane.toonshader` version `1.4.6` to the VCC/VPM listing.

## Preconditions

- `package.json` version is `1.4.6`
- Working tree is clean
- The branch to release from is pushed
- GitHub Actions is enabled for this repository

## Release Steps

1. Verify the current package version:
   ```powershell
   Get-Content package.json -TotalCount 20
   ```
2. Verify the working tree is clean:
   ```powershell
   git status --short
   ```
3. Create the release tag:
   ```powershell
   git tag v1.4.6
   ```
4. Push the tag:
   ```powershell
   git push origin v1.4.6
   ```
5. Confirm GitHub Actions `Build VPM Listing` completes successfully.
6. Confirm the GitHub Release contains `com.natane.toonshader-1.4.6.zip`.
7. Confirm the VPM listing shows `1.4.6`:
   - `https://natanetoon.com/index.json`

## Expected Output

- GitHub Release: `v1.4.6`
- Release zip: `com.natane.toonshader-1.4.6.zip`
- Updated VPM listing entry for `1.4.6`

## Rollback Notes

- If the tag was pushed by mistake:
  ```powershell
  git tag -d v1.4.6
  git push origin :refs/tags/v1.4.6
  ```
- If the workflow fails after tagging, fix the issue and recreate the tag only after confirming the old tag has been removed.
