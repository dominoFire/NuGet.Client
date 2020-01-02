using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.PackageManagement.VisualStudio;
using NuGet.ProjectManagement;
using Xunit.Abstractions;

namespace NuGet.PackageManagement.UI.Test
{
    /// <summary>
    /// Uses XUnit logging mechanism to log events from NuGetUILogger supported loggers
    /// </summary>
    internal class XUnitNuGetUILogger : INuGetUILogger
    {
        private readonly ITestOutputHelper output;

        public XUnitNuGetUILogger(ITestOutputHelper output)
        {
            this.output = output;
        }

        public void End()
        {
            throw new NotImplementedException();
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            output.WriteLine(message);
        }

        public void Log(ILogMessage message)
        {
            
        }

        public void ReportError(string message)
        {
            output.WriteLine(message);
        }

        public void ReportError(ILogMessage message)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }
    }
}
