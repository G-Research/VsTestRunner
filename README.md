# VsTestRunner

<img src="./flask.png" width="300px" />

A DotNet tool which can be used to run dotnet vstest across a set of assemblies.

### Prerequisites

* DotnetCore 5.0 Sdk

### Build the solution.

```build.py```

This is equivalent to running ```dotnet build VsTestRunner.sln``` and is the command run on the build server

### Testing

To run the unit tests:

```test.py```

On Linux this will skip the running of netcoreapp2.1 or net471 tests

To run the smoke tests:

```smoke_tests.py```

On Windows this will the run the tests in docker and also the tool in docker. 
On Linux, because on Jenkins docker in docker is prohibited, this just runs the tests with the test runner.

## Usage

VsTestRunner builds as a DotNet Command line tool and can be installed using `dotnet tool install`

### Supply list of test dlls

```vstest-runner --test-assemblies [path\to\test.dll,...]```

### Supply list of test dlls in a file

```vstest-runner --test-assemblies-file [path\to\test-list.txt]```

### Running tests in Docker

To run you tests in a docker image supply `--docker-image [name]` command line option

### Other command line options:

* `--max-concurrent-tests` to limit concurrency which defaults to number of cores.

* `--include-categories` test categories to include

* `--exclude-categories` test categories to exclude

* `--session-timeout [{double}]` assembly level test session timeout in minutes

* `--diagnostics` flag to turn on diagnostic vstest logging

* `--test-framework [framework-name]` force tests to run under specific framework i.e. `.NETFramework,Version=v4.8`

* `--test-platform [x86|x64|ARM]` force tests to run under specific test platform

## Versioning

Versioned using NerdBank.GitVersioning

