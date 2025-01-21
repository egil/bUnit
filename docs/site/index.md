---
uid: home
title: bUnit - a testing library for Blazor components
---

[![GitHub tag (latest SemVer pre-release)](https://img.shields.io/github/v/tag/egil/bunit?include_prereleases&logo=github&style=flat-square)](https://github.com/egil/bunit/releases)
[![Nuget](https://img.shields.io/nuget/dt/bunit?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit/)
[![Issues Open](https://img.shields.io/github/issues/egil/bunit.svg?style=flat-square&logo=github)](https://github.com/egil/bunit/issues)

# bUnit: a testing library for Blazor components

**bUnit** is a testing library for Blazor Components. Its goal is to make it easy to write _comprehensive, stable_ unit tests. With bUnit, you can:

- Setup and define components under tests using C# or Razor syntax
- Verify outcomes using semantic HTML comparer
- Interact with and inspect components as well as trigger event handlers
- Pass parameters, cascading values and inject services into components under test
- Mock `IJSRuntime`, Blazor authentication and authorization, and others

bUnit builds on top of existing unit testing frameworks such as xUnit, NUnit, MSTest and TUnit, which run the Blazor components tests in just the same way as any normal unit test. bUnit runs a test in milliseconds, compared to browser-based UI tests which usually take seconds to run.

**Go to the [Documentation](xref:getting-started) pages to learn more.**

### Test example

Let’s write a test for the `<Counter>` component listed below. This comes with the standard Blazor project template which verifies that the counter corrects increments when the button is clicked:

[!code-razor[Counter.razor](../samples/components/Counter.razor)]

To do this, you can carry out the following using bUnit and xUnit:

[!code-cshtml[CounterTest.razor](../samples/tests/razor/CounterTest.razor)]

This test uses bUnit's test context to render the `Counter` component with the `Render` method. It then finds the button the component rendered and clicks it with the `Find` and `Click` methods. Finally, it finds the paragraph (`<p>`) element and verifies that it matches the expected markup passed to the MarkupMatches method.

**Go to the [Documentation](xref:getting-started) pages to learn more.**

### NuGet downloads

bUnit is available on NuGet in various incarnations. Most users should just pick the [bUnit](https://www.nuget.org/packages/bunit/) package:

| Name | Description | NuGet Download Link |
| ----- | ----- | ---- |
| [bUnit](https://www.nuget.org/packages/bunit/) | Includes the bUnit.core and bUnit.web packages. | [![Nuget](https://img.shields.io/nuget/dt/bunit?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit/) |
| [bUnit.core](https://www.nuget.org/packages/bunit.core/) | Core library that enables rendering a Blazor component in a test context. | [![Nuget](https://img.shields.io/nuget/dt/bunit.core?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit.core/) |
| [bUnit.web](https://www.nuget.org/packages/bunit.web/) | Adds support for testing Blazor components for the web. This includes bUnit.core. | [![Nuget](https://img.shields.io/nuget/dt/bunit.web?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit.web/) |
| [bUnit.template](https://www.nuget.org/packages/bunit.template/) | Template for bUnit test projects based on xUnit, NUnit, MSTest or TUnit | [![Nuget](https://img.shields.io/nuget/dt/bunit.template?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit.template/) |
| [bUnit.generators](https://www.nuget.org/packages/bunit.generators/)|Source code generators to minimize code setup in various situations.|[![Nuget](https://img.shields.io/nuget/dt/bunit.generators?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit.generators/)|
| [bUnit.web.query](https://www.nuget.org/packages/bunit.web.query/)|bUnit implementation of testing-library.com's query APIs.|[![Nuget](https://img.shields.io/nuget/dt/bunit.web.query?logo=nuget&style=flat-square)](https://www.nuget.org/packages/bunit.web.query/)|

## Sponsors

A huge thank you to the [sponsors of bUnit](https://github.com/sponsors/egil). The higher tier sponsors are:

<div class="d-flex flex-row mb-3">
	<a href="https://github.com/syncfusion" class="d-block p-3 text-center">
		<img class="avatar avatar rounded-circle" src="https://avatars.githubusercontent.com/u/1699795?s=460" width="72" height="72" alt="@syncfusion" />
		<br />
		Syncfusion
	</a>
</div>

## Contributors

Shoutouts and a big thank you [to all the contributors](https://github.com/egil/bunit/graphs/contributors) to the library, including those who raise issues, those who provide input to issues, and those who send pull requests.

**Want to help out? You can help in a number of ways:**

- Provide feedback and input through [issues](https://github.com/egil/bunit/issues), via [Twitter](https://twitter.com/egilhansen) or by using the [bUnit Gitter chat room](https://gitter.im/egil/bunit).
- Help build the library. Just pick an issue and submit pull requests.
- Help write documentation.
- Create blog posts, presentations, or video tutorials. If you do, I'll be happy to showcase them in the [related section](xref:external-resources) on this site.

## Code of conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

## .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

