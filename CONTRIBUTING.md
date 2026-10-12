# Contributing to Simple Location Display

Thanks for taking the time to help out. Here is how to get changes in front of the right eyes.

## Found a bug?

- Search the [issues](https://github.com/Kkthnx-Stardew/SimpleLocationDisplay/issues) first, so the same thing isn't tracked twice.
- If nothing matches, [open a new one](https://github.com/Kkthnx-Stardew/SimpleLocationDisplay/issues/new/choose) with the Bug report template. It asks for a SMAPI log and the line `debug_location` prints in the affected place.

## Want to suggest a feature?

Open an issue with the Feature request template and describe the idea before you start writing code, so it can be talked through first.

## Adding a translation or a location name

- Run `sld_dump_missing_translations` in the SMAPI console after playing. It writes `i18n/missing.en.json` with a key for each place that fell back to a guessed name.
- Put the names you want in `SimpleLocationDisplay/i18n/default.json` for English, or in the file for your language (`de.json`, `fr.json` and so on). Keys look like `location.Custom_MyPlace`. For names that belong to another mod, a `DisplayName` in that mod's `Data/Locations` is the better fix.

## Writing a patch?

1. Fork the project and make a branch: `git checkout -b my-change`
2. Make your change and try it in game
3. Commit with a clear message: a short imperative subject, then a body that says what and why
4. Push your branch and open a pull request

Please keep the pull request focused on one thing and follow the style of the code around it. Logic that doesn't need the game belongs in `NameFormatting.cs` or `ModConfig.cs`, where the tests can reach it, and new behavior there should come with a test.

## Building and testing

You need the .NET SDK, Stardew Valley and SMAPI. The mod build package finds the game folder on its own and copies the built mod into `Mods`.

```bash
dotnet build SimpleLocationDisplay.sln -c Release
dotnet test SimpleLocationDisplay.Tests
```

The tests don't need the game. The release zip is written next to the built DLL in `SimpleLocationDisplay/bin/Release/net6.0`.

## Releasing

The version is set in one place, the `Version` property of `SimpleLocationDisplay/SimpleLocationDisplay.csproj`. The build copies it into the manifest. Add a section for it to `CHANGELOG.md`, and when you are ready to release, put the release date in that section's heading in place of "(not released)". Then run the script from the repo root:

```bash
powershell -ExecutionPolicy Bypass -File tools/release.ps1            # dry run: checks, builds, tests, prints the plan
powershell -ExecutionPolicy Bypass -File tools/release.ps1 -Publish   # tags, pushes, creates the GitHub release
```

The script stops if the tree is dirty, the tag already exists, the changelog has no dated entry for the version, the tests fail, or the `CF_API_KEY` secret is missing. It builds on your PC, because the build needs the game's files, and it uses your signed-in `gh` session to create the GitHub release with the zip attached.

Publishing the release starts the `Upload release files` workflow in `.github/workflows/publish.yml`. It downloads the zip from the release and sends it to two places. To check the workflow without uploading, run it from the Actions tab with "dry run" on.

- **CurseForge:** uploaded with `tools/upload-curseforge.ps1`, using the `CF_API_KEY` repository secret. A version with `-alpha.N`, `-beta.N` or `-rc.N` uploads as an alpha or beta.
- **Nexus Mods:** uploaded with Nexus's official upload action, which adds the zip as a new version of the existing main file. Only full releases go to Nexus, because that file's version drives the page and SMAPI's update alerts. It needs the secret `NEXUS_API_PERSONAL_KEY` and the variables `NEXUS_FILE_ID` and `NEXUS_MOD_ID` under Settings, Secrets and variables, Actions, and is skipped until they exist.

Nexus's upload API and its action are both in beta, so the action is pinned to an exact commit. The action uploads the file, the version and the changelog. Update the Nexus page description by hand from `docs/nexus-description.bbcode` when it changes.

A released version is never changed or re-tagged. If a release has a problem, fix it and ship the next version.

## License

Simple Location Display is released under the MIT license. See [LICENSE.txt](LICENSE.txt). Submitting a change means you are fine with it being part of the project under those terms.

Thanks for contributing.
