using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MVCChatSite.Controllers;

namespace MVCChatSite.Models
{
    public class HomeVM
    {
        public List<MessageInfo> Messages { get; set; }
    }
}