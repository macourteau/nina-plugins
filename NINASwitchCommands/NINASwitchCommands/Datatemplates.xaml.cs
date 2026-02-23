using System.ComponentModel.Composition;
using System.Windows;

namespace NINASwitchCommands {
    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates : ResourceDictionary {
        public Datatemplates() {
            InitializeComponent();
        }
    }
}
