using System;
using System.Collections.Generic;
using System.Linq;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core
{
    public class EnvironmentOptions
    {
        public bool ClearEnvironmentVariables { get; }

        public string BaseResultsDirectory { get; }
        public bool WriteToBaseResultsDirectory { get; }
        public bool RunTestInDocker { get; }

        public List<(string LocalDirectory, string MountDirectory)> AdditionalMounts { get; }

        public List<(string Key, string Value)> EnvironmentVariables { get; }

        public EnvironmentOptions(IEnvironmentOptions options)
        {
            EnvironmentVariables = SplitCommandLineVariable(options.EnvironmentVariables, '=');
            ClearEnvironmentVariables = options.RemoveAllEnvironmentVariables;
            BaseResultsDirectory = options.ResultsDirectory;
            WriteToBaseResultsDirectory = options.WriteToBaseResultsDirectory;

            if (options.DockerImage.Any())
            {
                RunTestInDocker = true;
                foreach (string image in options.DockerImage)
                {
                    var index = image.IndexOf('=');
                    if (index == -1)
                    {
                        if (_dockerImages.ContainsKey("*"))
                        {
                            throw new InvalidCommandLineException("DockerImages option is not correctly formed. Can only specify global image");
                        }
                        _dockerImages.Add("*", image);
                    }
                    else
                    {
                        _dockerImages.Add(image.Substring(0, index), image.Substring(index + 1));
                    }
                }

                AdditionalMounts = SplitCommandLineVariable(options.AdditionalMounts, '|');
            }
        }

        private Dictionary<string, string> _dockerImages = new Dictionary<string, string>();
        public bool TryGetDockerImage(string targetFramework, out string dockerImage)
        {
            // Should this be Regex?
            if (_dockerImages.TryGetValue(targetFramework, out dockerImage))
            {
                return true;
            }

            if (_dockerImages.TryGetValue("*", out dockerImage))
            {
                return true;
            }

            return false;
        }

        public List<(string, string)> SplitCommandLineVariable(IEnumerable<string> commandLineArgs, char splitChar)
        {
            var result = new List<(string, string)>();

            foreach (string arg in commandLineArgs)
            {
                var components = arg.Split(splitChar);

                if (components.Length != 2)
                {
                    throw new ArgumentException($"Invalid arg format, expected the format 'VARIABLE{splitChar}value': {arg}", nameof(commandLineArgs));
                }

                result.Add((components[0], components[1]));
            }

            return result;
        }
    }
}