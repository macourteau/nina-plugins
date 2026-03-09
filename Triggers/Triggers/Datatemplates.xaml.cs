using System.ComponentModel.Composition;
using System.Windows;

namespace Triggers {
    [Export(typeof(ResourceDictionary))]
    public partial class Datatemplates : ResourceDictionary {
        public Datatemplates() {
            InitializeComponent();
        }
    }
}
