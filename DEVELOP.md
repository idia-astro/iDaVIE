# Developing for iDaVIE
## Development environment
  * Unity
  * Powershell
  * Set up UnityYamlMerge.
  * [Jetbrains](https://www.jetbrains.com/rider/) or [VSCode](https://code.visualstudio.com/)
  * Debugging on [Jetbrains](https://www.jetbrains.com/help/rider/Debugging_Unity_Applications.html) or [VSCode](https://marketplace.visualstudio.com/items?itemName=visualstudiotoolsforunity.vstuc)
## Merging procedure
Merging two branches of a Unity project can be problematic and very time consuming. We list the steps to follow when merging two branches.
  * Switch to the target branch of iDaVIE with `git switch <target>`.
  * Merge with `git merge --no-commit <remote>`, where remote is the branch to be merged into the current branch.
  * If any conflict in script files occur, resolve using your mergetool of choice (we recommend VSCode or a derivative thereof).
  * Once all script file conflicts are resolved, resolve scene file conflicts (if any exist) using UnityYamlMerge.
  * Run `git mergetool` to run UnityYaml with the settings set up earlier.
  * If a temporary file conflicts, select the `current` option and complete the merge.
  * Test the final merged result.
## Testing regime
  * [User testing](https://docs.google.com/forms/d/e/1FAIpQLSf-gYZtDUkB3AV8zIZWm9QQ4w7NSWaDnXPDGA5uS65Yo5uVcw/viewform?usp=sf_link)
