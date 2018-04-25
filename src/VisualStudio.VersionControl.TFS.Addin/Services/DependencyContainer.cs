// DependencyContainer.cs
// 
// Authors:
//       Ventsislav Mladenov
//       Javier Suárez Ruiz
// 
// The MIT License (MIT)
// 
// Copyright (c) 2013-2018 Ventsislav Mladenov, Javier Suárez Ruiz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Autofac;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.TFS.Models;
using MonoDevelop.VersionControl.TFS.MonoDevelopWrappers;
using MonoDevelop.VersionControl.TFS.MonoDevelopWrappers.Implementation;

namespace MonoDevelop.VersionControl.TFS.Services
{
    public static class DependencyContainer
    {
        public static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<FileKeeperService>().As<IFileKeeperService>().SingleInstance();
            builder.RegisterType<TeamFoundationServerVersionControlService>().SingleInstance();
            builder.RegisterType<WorkspaceService>().As<IWorkspaceService>();
            builder.RegisterType<SoapInvoker>().As<ISoapInvoker>();

            Container = builder.Build();
        }

        public static IContainer Container { get; private set; }

        public static IWorkspaceService GetWorkspace(WorkspaceData workspaceData, ProjectCollection collection)
        {
            return Container.Resolve<IWorkspaceService>(new TypedParameter(typeof(WorkspaceData), workspaceData),
                                                 new TypedParameter(typeof(ProjectCollection), collection));
        }

        public static TeamFoundationServerRepository GetTeamFoundationServerRepository(string path, WorkspaceData workspaceData, ProjectCollection collection)
        {
            using (var scope = Container.BeginLifetimeScope())
            {
                var workspace = GetWorkspace(workspaceData, collection);
                return scope.Resolve<TeamFoundationServerRepository>(new NamedParameter("rootPath", path), new TypedParameter(typeof (IWorkspaceService), workspace));
            }
        }

        public static ISoapInvoker GetSoapInvoker(TFSService service)
        {
            return Container.Resolve<ISoapInvoker>(new TypedParameter(typeof(TFSService), service));
        }
    }

    public class ServiceBuilder : ContainerBuilder
    {
        public ServiceBuilder()
        {
            this.RegisterType<ProgressService>().As<IProgressService>().SingleInstance();
           
            this.Register<ConfigurationService>(ctx =>
            {
                var service = new ConfigurationService();
                service.Init(UserProfile.Current.ConfigDir);
                return service;
            }).As<IConfigurationService>().SingleInstance();

            this.RegisterType<LoggingService>().As<ILoggingService>().SingleInstance();
            this.RegisterType<NotificationService>().As<INotificationService>();
            this.RegisterType<TeamFoundationServerRepository>().InstancePerLifetimeScope().OnActivated(a => a.Instance.NotificationService = a.Context.Resolve<INotificationService>() );
        }
    }
}