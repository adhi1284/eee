# Instructions

## Use The Local Srcwalk Binary

`.github/srcwalk.exe` is the repository-local source of truth for code navigation and impact analysis.

Before any non-trivial task that reads, explains, modifies, reviews, or makes claims about code, run this command once without piping or truncating its output:

```powershell
.\.github\srcwalk.exe guide
```

When running from a Bash-compatible shell, use `./.github/srcwalk.exe` instead. Do not rerun `guide` recursively during the same task. Use `.\.github\srcwalk.exe --help` or `.\.github\srcwalk.exe <command> --help` only when command flags need confirmation.

If the local binary fails, report the failure before using a fallback. Never silently replace srcwalk with broad file reads, grep, glob, tree output, or model inference.

## Mandatory Navigation Rules

1. Start with `overview` to understand the repository or the narrowest relevant scope.
2. Use `discover` to identify exact files, symbols, or literal text.
3. Select one exact target before making code claims.
4. Use `context`, `show`, `trace`, `deps`, and `assess` to understand that target and its impact.
5. Follow the `> Next:` commands emitted by srcwalk instead of restarting with a broader search.
6. Keep `--scope` as narrow as possible.
7. Back substantive claims with an exact path, line range, symbol, relation, or byte span from srcwalk.
8. Treat syntax, text, and artifact evidence as static navigation evidence, not proof of runtime behavior.
9. After editing, run `review`, then run the relevant build or test command.
10. Use raw `rg` only for final regex or literal confirmation after srcwalk has established the target.

Do not use raw search to infer definitions, callers, callees, dependencies, runtime flow, or symbol ownership.

## Default Workflow

```powershell
.\.github\srcwalk.exe overview --scope .
.\.github\srcwalk.exe discover '<query>' --scope .
.\.github\srcwalk.exe context <symbol-or-file:line> --scope .
.\.github\srcwalk.exe show <path>:<line-range>
.\.github\srcwalk.exe trace callers <symbol> --scope .
.\.github\srcwalk.exe trace callees <symbol> --detailed --scope .
.\.github\srcwalk.exe deps <file>
.\.github\srcwalk.exe assess <symbol> --scope .
```

Use only the commands required to establish enough evidence for the task.

## Route Selection

Use these srcwalk routes instead of raw search:

- Repository map: `.\.github\srcwalk.exe overview --scope .`
- Symbols: `.\.github\srcwalk.exe overview --scope . --symbols`
- Symbol search: `.\.github\srcwalk.exe discover <symbol> --scope .`
- File search: `.\.github\srcwalk.exe discover '<glob>' --as file --scope .`
- Literal text: `.\.github\srcwalk.exe discover 'foo,bar' --match any --as text --scope .`
- Exact context: `.\.github\srcwalk.exe context <file>:<symbol> --scope .`
- Exact source: `.\.github\srcwalk.exe show <path>:<line-range>`
- Callers: `.\.github\srcwalk.exe trace callers <symbol> --scope .`
- Callees: `.\.github\srcwalk.exe trace callees <symbol> --detailed --scope .`
- Dependencies: `.\.github\srcwalk.exe deps <file>`
- Change risk: `.\.github\srcwalk.exe assess <symbol> --scope .`
- Working-tree review: `.\.github\srcwalk.exe review`
- Staged review: `.\.github\srcwalk.exe review --staged`

Do not pass a bare file to `context`; use `show` for exact file content.

## Large JavaScript Artifacts

Treat `.github/source-code.js` and other generated, bundled, minified, or very large JavaScript files as artifacts. Never dump or read the entire file first.

Start with:

```powershell
.\.github\srcwalk.exe overview --scope .github/source-code.js --artifact
.\.github\srcwalk.exe discover <symbol> --as symbol --scope .github/source-code.js --artifact
.\.github\srcwalk.exe context .github/source-code.js:<qualified-symbol> --scope .github/source-code.js --artifact
```

Then narrow with one of these routes:

```powershell
.\.github\srcwalk.exe .github/source-code.js --artifact --section <qualified-symbol>
.\.github\srcwalk.exe .github/source-code.js --artifact --section <start-line>-<end-line>
.\.github\srcwalk.exe .github/source-code.js --artifact --section bytes:<start>-<end>
.\.github\srcwalk.exe trace callers <qualified-symbol> --scope .github/source-code.js --artifact
.\.github\srcwalk.exe trace callees <qualified-symbol> --detailed --scope .github/source-code.js --artifact
```

Prefer qualified symbols when names repeat. Artifact definitions and call relations are parser-backed syntax evidence only; they do not prove runtime dispatch, reachability, aliases, or original source ownership.

## Editing Rules

- Read and understand the current flow before editing.
- Make the smallest correct change.
- Preserve unrelated user changes and never revert them.
- Do not add abstractions, compatibility layers, or broad refactors unless required.
- Keep network calls, file access, navigation, and mutations explicit.
- Do not claim a change works until review and validation complete.

## Validation

After source changes, run:

```powershell
.\.github\srcwalk.exe review
dotnet build .\ovfteam.com.csproj --no-restore
```

Use `git diff --check` for final whitespace validation. For documentation-only changes, run a scoped srcwalk review and `git diff --check -- <file>`.

If a command cannot run, report the exact limitation and the next-best validation performed.

