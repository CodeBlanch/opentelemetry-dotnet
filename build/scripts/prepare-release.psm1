$gitHubBotUserName="github-actions[bot]"
$gitHubBotEmail="41898282+github-actions[bot]@users.noreply.github.com"

function CreatePullRequestToUpdateChangelogsAndPublicApis {
  param(
    [Parameter(Mandatory=$true)][string]$minVerTagPrefix,
    [Parameter(Mandatory=$true)][string]$version,
    [Parameter(Mandatory=$true)][string]$gitRepository,
    [Parameter()][string]$gitUserName=$gitHubBotUserName,
    [Parameter()][string]$gitUserEmail=$gitHubBotEmail,
    [Parameter()][string]$targetBranch="main"
  )

  $tag="${minVerTagPrefix}${version}"
  $branch="release/prepare-${tag}-release"

  git config user.name $gitUserName
  git config user.email $gitUserEmail

  git switch --create $branch 2>&1 | % ToString
  if ($LASTEXITCODE -gt 0)
  {
      throw 'git switch failure'
  }

  $body =
@"
Note: This PR was opened automatically by the [prepare release workflow](https://github.com/$gitRepository/actions/workflows/prepare-release.yml).

## Changes

* CHANGELOG files updated for projects being released.
"@

  # Update CHANGELOGs
  & ./build/scripts/update-changelogs.ps1 -minVerTagPrefix $minVerTagPrefix -version $version

  # Update publicApi files for stable releases
  if ($version -notlike "*-alpha*" -and $version -notlike "*-beta*" -and $version -notlike "*-rc*")
  {
    & ./build/scripts/finalize-publicapi.ps1 -minVerTagPrefix $minVerTagPrefix

    $body += "`r`n* Public API files updated for projects being released (only performed for stable releases)."
  }

  git commit -a -m "Prepare repo to release $tag." 2>&1 | % ToString
  if ($LASTEXITCODE -gt 0)
  {
      throw 'git commit failure'
  }

  git push -u origin $branch 2>&1 | % ToString
  if ($LASTEXITCODE -gt 0)
  {
      throw 'git push failure'
  }

  gh pr create `
    --title "[repo] Prepare release $tag" `
    --body $body `
    --base $targetBranch `
    --head $branch `
    --label infra
}

Export-ModuleMember -Function CreatePullRequestToUpdateChangelogsAndPublicApis
