# Developing for iDaVIE
## Development environment
Before developing for iDaVIE, we strongly recommend that, in addition to the steps listed in the [Building from source](https://github.com/idia-astro/iDaVIE/blob/main/BUILD.md#prerequisites) document, the following steps are followed:
### Install a code editor
Unity will require the installation of [Visual Studio](https://visualstudio.microsoft.com/) as part of its installation, since that provides the compiler and libraries for the C# aspects of Unity. While it can serve as an IDE, we do not recommend it due to the heavy performance load it demands when in use. Instead, we recommend the following IDEs:
  * [Jetbrains](https://www.jetbrains.com/rider/)
  * [VSCode](https://code.visualstudio.com/)
    
They are both fully functional, and considerably more lightweight than Visual Studio. In addition, both have good integration with the Unity Editor to allow for debugging iDaVIE while it is running. Consult the following resources on how to set up debugging a Unity project in the respective IDEs:
  * [Debugging Unity Applications with Jetbrains](https://www.jetbrains.com/help/rider/Debugging_Unity_Applications.html)
  * [VSCode Tools for Unity](https://marketplace.visualstudio.com/items?itemName=visualstudiotoolsforunity.vstuc)
### Set up UnityYamlMerge
To facilitate the merging of separate branches that include merge conflicts in scene (`*.unity`) files in iDaVIE, Unity provides a mergetool called UnityYamlMerge. This requires a few set up steps before it can be used.
  1. First, the iDaVIE repo should be told to use UnityYamlMerge as the mergetool. Add the following lines to the `.git/commit` file. Note the escaped slash `\\` for folder divisors -- it will not work otherwise.
```
[mergetool "UnityYamlMerge"]
	cmd = '<path\\to\\Unity>\\Unity\\2021.3.47f1\\Editor\\Data\\Tools\\UnityYAMLMerge.exe' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
	trustExitCode = false
[merge]
	tool = UnityYamlMerge
```
  2. In the folder `<path\to\Unity>\Unity\2021.3.47f1\Editor\Data\Tools`, open the `mergespecfile.txt` text file. This file contains the fallback mergetools if UnityYamlMerge cannot resolve the conflicts or filetypes. Here we recommend VSCode as the fallback. Add the following lines to the `mergespecfile.txt` file. Note that `code` is likely in the system PATH if VSCode is installed, otherwise the path of the `code.exe` executable is required.
```
# VSCode
* use code --wait "%r" "%l" "b" "%d"
```
## Merging procedure
Merging two branches of a Unity project can be problematic and very time consuming if scene files were changed in both branches. We list the steps to follow when merging two branches.
  1. Switch to the target branch of iDaVIE with `git switch <target>`.
  2. Merge with `git merge --no-commit <remote>`, where remote is the branch to be merged into the current branch.
  3. If any conflict in script files occur, resolve using your mergetool of choice (we recommend VSCode or a derivative thereof).
  4. Once all script file conflicts are resolved, resolve scene file conflicts (if any exist) using UnityYamlMerge.
  5. Run `git mergetool` to run UnityYaml with the settings set up earlier.
  6. If a temporary file conflicts, select the `current` option and complete the merge.
  7. Test the final merged result using the testing protocol [checklist](https://forms.gle/ezLXLHeWR4ZeLmfz7).
## Testing regime
We provide a testing protocol [checklist](https://forms.gle/ezLXLHeWR4ZeLmfz7) that must be completed without issues before a change can be merged into the main branch. If a new feature is added, similar testing for that feature should be developed as well (i.e., how to test that feature, and what other features might be affected by it).
