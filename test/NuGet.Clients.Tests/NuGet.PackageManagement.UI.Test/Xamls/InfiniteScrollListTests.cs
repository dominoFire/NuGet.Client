// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Moq;
using NuGet.PackageManagement.VisualStudio;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.VisualStudio;
using Xunit;
using NuGet.Configuration;
using Xunit.Abstractions;
using NuGet.Protocol;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace NuGet.PackageManagement.UI.Test
{
    public class InfiniteScrollListTests
    {
        private readonly ITestOutputHelper output;

        public InfiniteScrollListTests(ITestOutputHelper output)
        {
            this.output = output;

            var joinableTaskContext = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);

            NuGetUIThreadHelper.SetCustomJoinableTaskFactory(joinableTaskContext.Factory);
        }

        [WpfFact]
        public void Constructor_JoinableTaskFactoryIsNull_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new InfiniteScrollList(joinableTaskFactory: null));

            Assert.Equal("joinableTaskFactory", exception.ParamName);
        }

        [WpfFact]
        public void CheckBoxesEnabled_Initialized_DefaultIsFalse()
        {
            var list = new InfiniteScrollList();

            Assert.False(list.CheckBoxesEnabled);
        }

        [WpfFact]
        public void DataContext_Initialized_DefaultIsItems()
        {
            var list = new InfiniteScrollList();

            Assert.Same(list.DataContext, list.Items);
        }

        [WpfFact]
        public void IsSolution_Initialized_DefaultIsFalse()
        {
            var list = new InfiniteScrollList();

            Assert.False(list.IsSolution);
        }

        [WpfFact]
        public void Items_Initialized_DefaultIsEmpty()
        {
            var list = new InfiniteScrollList();

            Assert.Empty(list.Items);
        }

        [WpfFact]
        public void PackageItems_Initialized_DefaultIsEmpty()
        {
            var list = new InfiniteScrollList();

            Assert.Empty(list.PackageItems);
        }

        [WpfFact]
        public void SelectedPackageItem_Initialized_DefaultIsNull()
        {
            var list = new InfiniteScrollList();

            Assert.Null(list.SelectedPackageItem);
        }

        [WpfFact]
        public async Task LoadItems_LoaderIsNull_Throws()
        {
            var list = new InfiniteScrollList();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                {
                    await list.LoadItemsAsync(
                        loader: null,
                        loadingMessage: "a",
                        logger: null,
                        searchResultTask: Task.FromResult<SearchResult<IPackageSearchMetadata>>(null),
                        token: CancellationToken.None);
                });

            Assert.Equal("loader", exception.ParamName);
        }

        [WpfTheory]
        [InlineData(null)]
        [InlineData("")]
        public async Task LoadItems_LoadingMessageIsNullOrEmpty_Throws(string loadingMessage)
        {
            var list = new InfiniteScrollList();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () =>
                {
                    await list.LoadItemsAsync(
                        Mock.Of<IPackageItemLoader>(),
                        loadingMessage,
                        logger: null,
                        searchResultTask: Task.FromResult<SearchResult<IPackageSearchMetadata>>(null),
                        token: CancellationToken.None);
                });

            Assert.Equal("loadingMessage", exception.ParamName);
        }

        [WpfFact]
        public async Task LoadItems_SearchResultTaskIsNull_Throws()
        {
            var list = new InfiniteScrollList();

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () =>
                {
                    await list.LoadItemsAsync(
                        Mock.Of<IPackageItemLoader>(),
                        loadingMessage: "a",
                        logger: null,
                        searchResultTask: null,
                        token: CancellationToken.None);
                });

            Assert.Equal("searchResultTask", exception.ParamName);
        }

        [WpfFact]
        public async Task LoadItems_IfCancelled_Throws()
        {
            var list = new InfiniteScrollList();

            await Assert.ThrowsAsync<OperationCanceledException>(
                async () =>
                {
                    await list.LoadItemsAsync(
                        Mock.Of<IPackageItemLoader>(),
                        loadingMessage: "a",
                        logger: null,
                        searchResultTask: Task.FromResult<SearchResult<IPackageSearchMetadata>>(null),
                        token: new CancellationToken(canceled: true));
                });
        }

        [WpfFact]
        public async Task LoadItems_BeforeGettingCurrent_WaitsForInitialResults()
        {
            var loader = new Mock<IPackageItemLoader>(MockBehavior.Strict);
            var state = new Mock<IItemLoaderState>();
            var hasWaited = false;

            loader.SetupGet(x => x.IsMultiSource)
                .Returns(true);
            loader.SetupGet(x => x.State)
                .Returns(state.Object);
            loader.Setup(x => x.UpdateStateAndReportAsync(
                    It.IsNotNull<SearchResult<IPackageSearchMetadata>>(),
                    It.IsNotNull<IProgress<IItemLoaderState>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(0));

            var loadingStatus = LoadingStatus.Loading;
            var loadingStatusCallCount = 0;

            state.Setup(x => x.LoadingStatus)
                .Returns(() => loadingStatus)
                .Callback(() =>
                    {
                        ++loadingStatusCallCount;

                        if (loadingStatusCallCount >= 2)
                        {
                            loadingStatus = LoadingStatus.NoItemsFound;
                            hasWaited = true;
                        }
                    });

            var itemsCount = 0;
            var itemsCountCallCount = 0;

            state.Setup(x => x.ItemsCount)
                .Returns(() => itemsCount)
                .Callback(() =>
                    {
                        ++itemsCountCallCount;

                        if (itemsCountCallCount >= 2)
                        {
                            itemsCount = 1;
                            hasWaited = true;
                        }
                    });

            loader.Setup(x => x.UpdateStateAsync(
                    It.IsNotNull<IProgress<IItemLoaderState>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));

            var logger = new Mock<INuGetUILogger>();
            var searchResultTask = Task.FromResult(new SearchResult<IPackageSearchMetadata>());

            using (var joinableTaskContext = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current))
            {
                var list = new InfiniteScrollList(new Lazy<JoinableTaskFactory>(() => joinableTaskContext.Factory));
                var taskCompletionSource = new TaskCompletionSource<string>();

                // Despite LoadItems(...) being a synchronous method, the method internally fires an asynchronous task.
                // We'll know when that task completes successfully when the LoadItemsCompleted event fires,
                // and to avoid infinite waits in exceptional cases, we'll interpret a call to reset as a failure.
                list.LoadItemsCompleted += (sender, args) => taskCompletionSource.TrySetResult(null);

                loader.Setup(x => x.Reset());
                logger.Setup(x => x.Log(
                        It.Is<MessageLevel>(m => m == MessageLevel.Error),
                        It.IsNotNull<string>(),
                        It.IsAny<object[]>()))
                    .Callback<MessageLevel, string, object[]>(
                        (messageLevel, message, args) =>
                            {
                                taskCompletionSource.TrySetResult(message);
                            });
                loader.Setup(x => x.GetCurrent())
                    .Returns(() =>
                    {
                        if (!hasWaited)
                        {
                            taskCompletionSource.TrySetResult("GetCurrent() was called before waiting for initial results.");
                        }

                        return Enumerable.Empty<PackageItemListViewModel>();
                    });

                await list.LoadItemsAsync(
                    loader.Object,
                    loadingMessage: "a",
                    logger: logger.Object,
                    searchResultTask: searchResultTask,
                    token: CancellationToken.None);

                var errorMessage = await taskCompletionSource.Task;

                Assert.Null(errorMessage);

                loader.Verify();
            }
        }


        // create the view models and pass it on
        [WpfFact]
        public void ScrollingBug_Repro_Fixed()
        {
            var urls = new string[]
            {
                "https://api.nuget.org/v3-flatcontainer/system.xml.xmldocument/4.3.0/icon",
                "https://www.nuget.org/Content/gallery/img/default-package-icon.svg",
                "https://api.nuget.org/v3-flatcontainer/serilog/2.9.1-dev-01154/icon",
                "https://api.nuget.org/v3-flatcontainer/microsoft.data.odata/5.8.4/icon",
                "https://api.nuget.org/v3-flatcontainer/awssdk.core/3.3.104.11/icon",
                "https://raw.github.com/App-vNext/Polly/master/Polly.png",
                "https://example.url/no-icon.png"
            };

            var items = urls.Select((i) => new PackageItemListViewModel()
            {
                IconUrl = new Uri(i),
                Id = "TestPackage"
            });
            var itemsObs = new ObservableCollection<PackageItemListViewModel>(items);

            var list = new InfiniteScrollList();
            list.ApplyTemplate();
            var listBox = list.FindName("_list") as InfiniteScrollListBox;
            listBox.ItemsSource = itemsObs;
            listBox.ApplyTemplate();
            var listTemplate = listBox.Template;
            var scrollControl = listTemplate.FindName("scroll", listBox) as ScrollViewer;

            Assert.NotNull(listBox);
            Assert.NotNull(scrollControl);

            for(int i = 0; i < 100; i++)
                scrollControl.PageDown();

            for (int i = 0; i < 30; i++)
                scrollControl.PageUp();

            NavigateTree(listBox, 0);
        }

        private void NavigateTree(DependencyObject dObj, int depth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Insert(0, " ", depth);
            sb.Append(dObj);
            output.WriteLine(sb.ToString());

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dObj); i++)
            {
                var child = VisualTreeHelper.GetChild(dObj, i);
                NavigateTree(child, depth + 1);
            }
        }

        /*
        // simulate a real search
        // do scroll up and down
        // intercept the event
        // seee results
        [WpfFact]
        public async Task ScrollBug_Repro_Search()
        {
            output.WriteLine("hola");
            var uiLogger = new XUnitNuGetUILogger(output);

            var solutionManager = Mock.Of<IVsSolutionManager>();
            var uiContext = Mock.Of<INuGetUIContext>();
            Mock.Get(uiContext)
                .Setup(x => x.SolutionManager)
                .Returns(solutionManager);

            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            var sourceRepository = Repository.Factory.GetCoreV3(packageSource.Source);

            var sourceRepos = new SourceRepository[] {
                sourceRepository
            };

            var feed = new MultiSourcePackageFeed(
                sourceRepositories: sourceRepos,
                logger: uiLogger,
                telemetryService: null);

            var loadCtx = new PackageLoadContext(
                sourceRepositories: sourceRepos,
                isSolution: false,
                uiContext: uiContext);

            var loader = new PackageItemLoader(
                context: loadCtx,
                packageFeed: feed,
                searchText: "*",
                includePrerelease: true);

            var tokenSource = new CancellationTokenSource();

            var searchTask = loader.SearchAsync(continuationToken: null, tokenSource.Token);

            using (var jtc = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current))
            {
                var list = new InfiniteScrollList(new Lazy<JoinableTaskFactory>( () => jtc.Factory ));

                await list.LoadItemsAsync(
                    loader: loader,
                    loadingMessage: "... (test) Loading ...",
                    logger: uiLogger,
                    searchResultTask: searchTask, // no search
                    token: tokenSource.Token);

                Assert.True(list.PackageItems.Count() > 0);
            }
        }
        */
    }
}
