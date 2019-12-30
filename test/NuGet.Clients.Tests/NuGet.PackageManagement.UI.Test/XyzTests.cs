using System.Windows.Controls;
using Xunit;

namespace NuGet.PackageManagement.UI.Test
{
    public class XyzTests
    {
        [WpfFact]
        public void ImageControl_StyleApplied()
        {
            var itemControl = new PackageItemControl();

            Assert.NotNull(itemControl);
        }
    }
}
