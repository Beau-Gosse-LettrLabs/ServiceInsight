﻿namespace ServiceInsight.Tests
{
    using System.Threading.Tasks;
    using Autofac;
    using Caliburn.Micro;
    using NSubstitute;
    using NUnit.Framework;
    using ServiceInsight.Framework.Settings;
    using ServiceInsight.ServiceControl;
    using ServiceInsight.Settings;
    using ServiceInsight.Shell;
    using Shouldly;

    [TestFixture]
    public class ServiceControlConnectionDialogTests
    {
        IServiceControl serviceControl;
        ShellViewModel shell;
        ISettingsProvider settingsProvider;
        IContainer container;
        ServiceControlConnectionProvider connection;
        ProfilerSettings storedSetting;
        ServiceControlConnectionViewModel connectTo;

        [SetUp]
        public void TestInitialize()
        {
            shell = Substitute.For<ShellViewModel>();
            serviceControl = Substitute.For<IServiceControl>();
            settingsProvider = Substitute.For<ISettingsProvider>();
            connection = Substitute.For<ServiceControlConnectionProvider>();
            container = RegisterContainer();
            storedSetting = GetReloadedSettings();
            settingsProvider.GetSettings<ProfilerSettings>().Returns(storedSetting);
            connectTo = new ServiceControlConnectionViewModel(settingsProvider, container) { Parent = shell }; //TODO: Do we need to pass the full container here?
        }

        [Test]
        public async Task Should_be_able_to_connect_to_a_valid_service()
        {
            ((IActivate)connectTo).Activate();
            connectTo.ServiceUrl = "http://localhost:8080/managemnetApi";
            await connectTo.Accept();

            connectTo.CanAccept().ShouldBe(true);
            connectTo.ShowError.ShouldBe(false);
        }

        [Test]
        public async Task Should_store_connection_address_and_add_it_to_recent_entries()
        {
            ((IActivate)connectTo).Activate();
            connectTo.ServiceUrl = "http://localhost:8080/managemnetApi";
            await connectTo.Accept();

            settingsProvider.Received().SaveSettings(Arg.Any<ProfilerSettings>());
            storedSetting.RecentServiceControlEntries.Count.ShouldBe(3);
            storedSetting.RecentServiceControlEntries.ShouldContain("http://localhost:8080/managemnetApi");
        }

        ProfilerSettings GetReloadedSettings()
        {
            var settings = new ProfilerSettings();

            settings.RecentServiceControlEntries.Add("http://localhost/api");
            settings.RecentServiceControlEntries.Add("http://othermachine:8888/api");

            return settings;
        }

        IContainer RegisterContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(serviceControl);
            builder.RegisterInstance(connection);

            return builder.Build();
        }
    }
}