# Firebend JSON Patch Generator
A class library for comparing objects and generating JSON patch documents.

## Building from source

- Building requires the .NET SDK version in [`global.json`](global.json) (10.0.401 or a later 10.0.4xx patch). Running the tests also needs the .NET 9 runtime.
- Target frameworks are set in [`Directory.Build.props`](Directory.Build.props). `FirebendTargetFrameworks` lists the frameworks every library and test project builds for (currently `net9.0;net10.0`). `FirebendAppTargetFramework` is the single framework for samples and other runnable apps.
- Package versions are managed centrally in [`Directory.Packages.props`](Directory.Packages.props). Microsoft framework packages have one version block per target framework, so adding a framework means adding it to `FirebendTargetFrameworks` and adding a matching block there.
