using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickDeploy.Common.Messages
{
    [Serializable]
    public class ChangeServerModulesStatusRequest : AuthorizedRequest
    {
        public string Server { get; set; }

        public string Port { get; set; }

        public string Rubrik { get; set; }

        public string SystemConnection { get; set; }

        public ServerModuleStatus DesiredServerModuleStatus { get; set; }
    }
}
