# DEADSHOT.io Windows client

A Windows desktop client built with C#, Windows Forms, and Microsoft WebView2, with a companion HTML guide and JavaScript package assets.

## Repository overview

| File | Purpose |
| :--- | :--- |
| [Program.cs](../Program.cs) | Application entry point and WebView2 integration. |
| [ovfteam.com.csproj](../ovfteam.com.csproj) | .NET project definition. |
| [index.html](../index.html) | Companion HTML page. |
| [assets/](../assets/) | Packaged client assets. |
| [installer.iss](../installer.iss) | Inno Setup installer definition. |
| [source-code.js](source-code.js) | Existing JavaScript source used in the package workflow. |

## Technology

C# · Windows Forms · WebView2 · HTML · JavaScript

## Updating the JavaScript package

1. Download [source-code.js](source-code.js) and read the [existing guide](https://adhi1284.github.io/eee).
2. Open the file in your code editor and make your intended changes.
3. Open the [existing package build service](https://vai-lon-ds.ovfteam.com/).
4. Upload the edited `source-code.js` in the **client** field and wait for the build to finish.
5. Download the resulting `final.pkg`.
6. Replace [assets/final.pkg](../assets/final.pkg) with the newly built package.
7. Restart the client to load the updated package.

## Package requirements

- Keep the output filename as `final.pkg`.
- Place it in the `assets` directory next to the client.
- Restart the client whenever the package is replaced.

## Project context

The client window is branded **OVFTEAM.COM - DEADSHOT.io**. See the [root project overview](../README.md) for additional context. Referenced third-party names retain their respective ownership.
