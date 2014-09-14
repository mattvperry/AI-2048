using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using CommandLine;
using CommandLine.Text;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyCopyright("Copyright © Matt Perry 2014")]

namespace AI_2048
{
    public enum AIMode
    {
        Client,
        Server
    }

    class Options
    {
        [Option('k', "keep-playing", DefaultValue = true, HelpText = "Keep running after 2048 has been achieved")]
        public bool KeepPlaying { get; set; }

        [Option('m', "mode", DefaultValue = AIMode.Client, HelpText = "Mode to run AI in. Server or Client")]
        public AIMode AIMode { get; set; }

        [Option('p', "port", DefaultValue = 8001, HelpText = "Port to run server on.")]
        public int Port { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
