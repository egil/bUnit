---
uid: create-test-project
title: Creating a new bUnit test project
---

# Creating a new bUnit test project

To write tests, you need a place to put them - a test project. bUnit is not a unit test runner, so a general-purpose test framework like xUnit, NUnit, or MSTest is needed in addition to bUnit in order to write and run tests.

To use bUnit, the easiest approach is to use the bUnit project template described in the following section. To create a test project manually and in a general-purpose testing frameworks agnostic way, read the section [Creating a test project manually](##creating-a-test-project-manually) section further down the page.

## Creating a test project with bUnit template

To quickly get started with bUnit, install and use the [bUnit test project template](https://www.nuget.org/packages/bunit.template/).

The steps for creating a test project with the bUnit template are as follows:

1. Install the template (only needed the first time)
2. Create a new bUnit test project
3. Add the test project to your solution

These steps look like this from the `dotnet` CLI:

**1. Install the template**

Install the template from NuGet using this command:

```dotnetcli
dotnet new install bunit.template
```

**2. Create a new test project**

If you successfully installed the template listed in the previous section, you
can create a new project directly from the "Create new project" wizard in Visual Studio (or Rider), where the bUnit project type will also show up.

Otherwise, use one of the following command to create a bUnit test project with
the framework of your choice:

# [xUnit](#tab/xunit)

```dotnetcli
dotnet new bunit --framework xunit -o <NAME OF TEST PROJECT>
```

# [xUnit v3](#tab/xunitv3)

```dotnetcli
dotnet new bunit --framework xunitv3 -o <NAME OF TEST PROJECT>
```

# [NUnit](#tab/nunit)

```dotnetcli
dotnet new bunit --framework nunit -o <NAME OF TEST PROJECT>
```


# [MSTest](#tab/mstest)

```dotnetcli
dotnet new bunit --framework mstest -o <NAME OF TEST PROJECT>
```

***

The `--framework` option in the `dotnet new` command above is used to specify the unit testing framework used by the test project. If the `--framework` option is omitted, the default test framework `xunit` will be configured. Currently supported options are the following:

- `xunit` and `xunitv3` - [xUnit](https://xunit.net/),
- `nunit` - [NUnit](https://nunit.org/),
- `mstest` - [MSTest](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- `tunit` - [TUnit](https://github.com/thomhurst/TUnit)

**3. Add the test project to your solution**

If using Visual Studio, add the test project to your solution (`.sln`), and add a reference between the test project and the project containing the components that should be tested:

```dotnetcli
dotnet sln <NAME OF PROJECT>.sln add <NAME OF TEST PROJECT>
dotnet add <NAME OF TEST PROJECT>.csproj reference <NAME OF COMPONENT PROJECT>.csproj
```

This will allow the test project to see and test the components in the component project.

## Creating a test project manually

This section will take you through the steps required to create a project for testing Blazor components using bUnit. Any of the three general-purpose test frameworks shown in step 1 below can be used. Briefly, here is what we will do:

1. Create a new xUnit/NUnit/MSTest/TUnit testing project
2. Add bUnit to the test project
3. Configure project settings
4. Add the test project to your existing solution

Let's look at these in more detail. These steps look like this from the 'dotnet' CLI:

**1. Create a new test project**

Use the following command (_click on the tab for the test framework of choice_):

# [xUnit](#tab/xunit)

```dotnetcli
dotnet new xunit -o <NAME OF TEST PROJECT>
```

# [xUnit v3](#tab/xunitv3)

```dotnetcli
dotnet new xunit3 -o <NAME OF TEST PROJECT>
```

# [NUnit](#tab/nunit)

```dotnetcli
dotnet new nunit -o <NAME OF TEST PROJECT>
```

# [MSTest](#tab/mstest)

```dotnetcli
dotnet new mstest -o <NAME OF TEST PROJECT>
```

***

The `-o` option in the `dotnet new` command above is used to specify the name of the test project.

**2. Add bUnit to the test project**

To add bUnit to the test project, change to the newly created test projects folder and use the following command:

```dotnetcli
cd <NAME OF PROJECT>
dotnet add package bunit --version #{NBGV_NuGetPackageVersion}#
```

**3. Configure project settings**

The test projects setting needs to be set to the following:

- the project's SDK needs to be set to `Microsoft.NET.Sdk.Razor` (this does not work with **TUnit** - a more detailed explanation can be found below)
- set the `<TargetFramework>` to `net8.0`

> [!NOTE]
> bUnit works with `net8.0` and above as well.

To do so, change the first part of the test projects `.csproj` file to look like this.:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>  ...

</Project>
```

**4. Add the test project to your solution**

If using Visual Studio, add the test project to your solution (`.sln`), and add a reference between the test project and the project containing the components that are to be tested:

```dotnetcli
dotnet sln <NAME OF PROJECT>.sln add <NAME OF TEST PROJECT>
dotnet add <NAME OF TEST PROJECT>.csproj reference <NAME OF COMPONENT PROJECT>.csproj
```

The result should be a test project with a `.csproj` that looks like this (non bUnit packages may have different version numbers):

# [xUnit](#tab/xunit)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="#{RELEASE_VERSION}#" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
		<PackageReference Include="xunit" Version="2.9.4" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="<PATH TO COMPONENT LIB>.csproj" />
  </ItemGroup>

</Project>
```

# [xUnit v3](#tab/xunitv3)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="#{RELEASE_VERSION}#" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
		<PackageReference Include="xunit.v3" Version="1.0.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="<PATH TO COMPONENT LIB>.csproj" />
  </ItemGroup>

</Project>
```

# [NUnit](#tab/nunit)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="#{RELEASE_VERSION}#" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="NUnit" Version="4.3.2" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="<PATH TO COMPONENT LIB>.csproj" />
  </ItemGroup>

</Project>
```

# [MSTest](#tab/mstest)

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="#{RELEASE_VERSION}#" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.7.1" />
    <PackageReference Include="MSTest.TestFramework" Version="3.7.1" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="<PATH TO COMPONENT LIB>.csproj" />
  </ItemGroup>

</Project>
```

# [TUnit](#tab/tunit)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestingPlatformApplication>false</IsTestingPlatformApplication>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="#{RELEASE_VERSION}#" />
    <PackageReference Include="TUnit" Version="0.6.154" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="<PATH TO COMPONENT LIB>.csproj" />
  </ItemGroup>

</Project>
```

> [!WARNING]
> **TUnit** and the `Microsoft.NET.Sdk.Razor` both utilize source code generators. Source generators can not see or interact with the output of another generator. Therefore **TUnit** does not work with `razor` files. Using `cs` based tests is working perfectly fine. For more information regarding the setup of **TUnit** head over to: https://github.com/thomhurst/TUnit

***

## Further reading

To start creating tests, continue reading the <xref:writing-tests> page.
