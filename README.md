# DEADSHOT.io Windows client

A Windows desktop client built with C#, Windows Forms, and Microsoft WebView2. The repository also includes an HTML guide and JavaScript package assets.

## Project structure

| Path | Purpose |
| :--- | :--- |
| [Program.cs](Program.cs) | Application entry point, desktop window, and WebView2 integration. |
| [ovfteam.com.csproj](ovfteam.com.csproj) | .NET project definition. |
| [index.html](index.html) | Companion HTML page. |
| [assets/](assets/) | Packaged client assets. |
| [installer.iss](installer.iss) | Inno Setup installer definition. |
| [.github/](.github/) | Existing supporting documentation and JavaScript source. |

## Technology

C# · Windows Forms · WebView2 · HTML · JavaScript

## Existing package workflow

The existing supporting documentation describes editing `.github/source-code.js`, rebuilding its package through the referenced build service, and placing the resulting `final.pkg` in the `assets` directory before restarting the client. Keep the required filename and location when following that workflow.

Refer to the [existing supporting documentation](.github/) for its original details. This README provides an overview without changing the implementation or build process.

## Project context

The client window is branded **OVFTEAM.COM - DEADSHOT.io**. This project is separate from Kerala Egg. DEADSHOT.io and other referenced third-party names retain their respective ownership.
