# Publishing MultiplayerHost to NuGet

## Version source

`host/Host.csproj` sets the version used for local builds. The publishing workflow
uses the pushed Git tag as the release version: `v1.5.5` produces package `1.5.5`.
It passes `-p:Version=1.5.5` to build, test, and pack so the assembly and package
versions match. No separate version-generation tool is used.

Choose an unused version and update `<Version>` in `host/Host.csproj` to match it.
The examples below use **1.5.5** because **1.5.4 has already been published**.
NuGet package versions are immutable; never move a published release tag or try
to replace an existing package with different contents.

## Validate and merge the release

Use a release branch and a pull request. The protected `main` branch requires
the **Build** status check before merging.

From the repository root, restore and build with .NET 11:

```sh
dotnet restore MultiplayerHost_host-only.sln
dotnet build MultiplayerHost_host-only.sln --configuration Release --no-restore
```

Run the same test runner used in CI. In PowerShell:

```powershell
$env:DOTNET_TEST_RUNNER = 'Microsoft.Testing.Platform'
dotnet test tests/MultiplayerHost.Tests/MultiplayerHost.Tests.csproj --configuration Release --no-build
```

In Bash, prefix the test command with
`DOTNET_TEST_RUNNER=Microsoft.Testing.Platform` instead.

Commit the implementation, tests, documentation, and project version. Push the
branch, open a pull request against `main`, wait for the checks to pass, and merge.

## Tag the merged commit

With a clean checkout, update local `main` and tag the merged release commit:

```sh
git switch main
git pull --ff-only origin main
git tag -a v1.5.5 -m "MultiplayerHost 1.5.5"
git push origin refs/tags/v1.5.5
```

The tag must point to the commit containing the release changes. If `main` has
advanced, pass the intended merged commit SHA to `git tag` explicitly.

## Automatic NuGet publication

Pushing a tag matching `v*.*.*` starts
[Publish to NuGet](https://github.com/enriko-riba/multiplayer-host/actions/workflows/publish.yml).
The workflow restores, builds, tests, packs, signs in to NuGet, pushes the package,
and archives the `.nupkg` as a workflow artifact for 30 days.

Authentication uses `NuGet/login@v1` with NuGet user `eriba`, the GitHub environment
`publish.yml`, and `id-token: write`. The NuGet trusted-publishing policy must match
this repository, `.github/workflows/publish.yml`, and that environment. It supplies
a short-lived API key; no permanent NuGet API key is needed locally.

Wait for the publish job to succeed, including **Push to NuGet**. NuGet validation
and indexing can delay restore availability after the upload has succeeded.
Verify the version on [NuGet.org](https://www.nuget.org/packages/MultiplayerHost)
and restore a consumer using NuGet.org and an empty package directory to avoid
mistaking a locally packed package for the published release:

```sh
dotnet restore path/to/consumer.csproj --packages ./artifacts/clean-nuget-cache --no-http-cache
```

Once verified, update consuming projects to the published version.

## Retrying and troubleshooting

- For transient workflow failures, rerun the failed job. The push uses
  `--skip-duplicate`, so an already published package is not overwritten.
- **Run workflow** must target an existing version tag. Dispatching from `main`
  fails intentionally because it does not identify a release version.
- For authentication failures, check the trusted-publishing policy and environment
  names above. Keep `id-token: write` enabled.
- For a successful upload followed by a restore error, wait for NuGet indexing and
  retry without the HTTP cache. Do not publish different contents under the same version.
- Fix released code with a new version and a new tag. Workflow-only maintenance
  applies to future tags; the workflow stored in an existing release tag is unchanged.
