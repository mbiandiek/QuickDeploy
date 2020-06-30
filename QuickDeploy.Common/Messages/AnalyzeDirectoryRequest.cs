using System;
using System.Collections.Generic;

namespace QuickDeploy.Common.Messages
{
    [Serializable]
    public class AnalyzeDirectoryRequest : AuthorizedRequest
    {
        public string Directory { get; set; }

        public List<string> IgnoreFiles { get; set; }
    }
}
