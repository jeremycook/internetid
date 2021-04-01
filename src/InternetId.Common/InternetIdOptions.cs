using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetId.Common
{
    public class InternetIdOptions
    {
        public string Title { get; set; }
        public string FromEmailAddress { get; set; }
        public string EmailFormat { get; set; } = "<h1>{0}</h1><div>{1}</div>";
    }
}
