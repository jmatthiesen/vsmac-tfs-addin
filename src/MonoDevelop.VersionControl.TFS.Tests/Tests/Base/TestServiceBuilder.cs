using System;
using Autofac;
using MonoDevelop.VersionControl.TFS.Services;

namespace MonoDevelop.VersionControl.TFS.Tests
{
    internal sealed class TestServiceBuilder : ContainerBuilder
    {
        public TestServiceBuilder()
        {
            this.RegisterType<TestProgressService>().As<IProgressService>().SingleInstance();
            this.Register<ConfigurationService>(ctx =>
            {
                var service = new ConfigurationService();
                service.Init(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

                return service;
            }).As<IConfigurationService>().SingleInstance();
            this.RegisterType<TestLoggingService>().As<ILoggingService>().SingleInstance();
        }
    }
}