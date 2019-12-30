import os
import subprocess
import sys

os.chdir(os.path.dirname(__file__))
print(f"Current working directory is {os.getcwd()}")
if sys.platform == 'win32':
    subprocess.run(["dotnet", "test", "./VsTestRunner.sln", "--no-build", "--configuration=Release"], check=True)
else:
    # We can't run net framework or netcoreapp2.1 tests on linux
    subprocess.run(["dotnet", "test", "./VsTestRunner.sln", "--no-build", "--configuration=Release", "--framework=netcoreapp3.1"], check=True)
    subprocess.run(["dotnet", "test", "./VsTestRunner.sln", "--no-build", "--configuration=Release", "--framework=net5.0"], check=True)
    subprocess.run(["dotnet", "test", "./VsTestRunner.sln", "--no-build", "--configuration=Release", "--framework=net6.0"], check=True)
