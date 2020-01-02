using System;
using Xunit;

namespace NuGet.PackageManagement.UI.Test
{
    public class PackageItemControlTests
    {
        [WpfFact]
        public void ImageControl_StyleApplied()
        {
            var itemControl = new PackageItemControl();
            var viewModel = new PackageItemListViewModel();

            viewModel.IconUrl = new Uri("http://sample.url/icon.png");
            itemControl.DataContext = viewModel;

            Assert.NotNull(itemControl);

            var controlStyle = itemControl.FindResource("controlStyle");
                       
            Assert.NotNull(controlStyle);
        }
    }
}
