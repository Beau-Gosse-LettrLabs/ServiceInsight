﻿namespace ServiceInsight.Framework.Modules
{
    using System;
    using Caliburn.Micro;
    using ServiceInsight.Framework.Settings;
    using System.Collections.Generic;
    using System.Xml;
    using Autofac;
    using ServiceInsight.Models;
    using ServiceInsight.ServiceControl;
    using ServiceInsight.Framework.Licensing;
    using ServiceInsight.Framework.MessageDecoders;
    using ServiceInsight.Startup;
    using System.Security;

    public class CoreModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StringContentDecoder>().As<IContentDecoder<string>>();
            builder.RegisterType<XmlContentDecoder>().As<IContentDecoder<XmlDocument>>();
            builder.RegisterType<HeaderContentDecoder>().As<IContentDecoder<IList<HeaderInfo>>>();
            builder.RegisterType<NetworkOperations>().SingleInstance();
            builder.RegisterType<AppLicenseManager>().SingleInstance();
            builder.RegisterType<CommandLineArgParser>().SingleInstance();
            builder.RegisterType<WorkNotifier>().As<IWorkNotifier>().InstancePerLifetimeScope();
            builder.RegisterType<ServiceControlConnectionProvider>().InstancePerDependency();
            builder.RegisterType<DefaultServiceControl>().As<IServiceControl>().InstancePerDependency();
            builder.RegisterType<ServiceControlClientRegistry>().AsSelf().SingleInstance();
            builder.Register<Func<string, string, SecureString, IServiceControl>>(c =>
            {
                var context = c.Resolve<IComponentContext>();

                return (url, username, password) =>
                {
                    var connectionProvider = new ServiceControlConnectionProvider();
                    connectionProvider.ConnectTo(url, username, password);

                    return context.Resolve<IServiceControl>(new TypedParameter(typeof(ServiceControlConnectionProvider), connectionProvider));
                };
            });
        }
    }
}