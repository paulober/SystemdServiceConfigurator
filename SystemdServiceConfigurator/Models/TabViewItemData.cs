using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemdServiceConfigurator.Models
{
    public class TabViewItemData
    {
        public string Header { get; set; }
        public SymbolIconSource IconSource { get; set; } = null;
        public object Content { get; set; }
    }
}
