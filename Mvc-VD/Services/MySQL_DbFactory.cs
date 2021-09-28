using Autofac;
using Autofac.Integration.Mvc;
using Mvc_VD.Models;
using Mvc_VD.Services.TIMS;
using System;
using System.Reflection;
using System.Web.Mvc;

namespace Mvc_VD.Services
{
    public class MySQL_DbFactory
    {
        public static void Builder()
        {
            SetAutofacContainer();
        }

        private static void SetAutofacContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(Assembly.GetExecutingAssembly());
            builder.RegisterType<DbFactory>().As<IDbFactory>().InstancePerRequest();
            builder.RegisterType<WOService>().As<IWOService>().InstancePerRequest();
            builder.RegisterType<DMSService>().As<IDMSService>().InstancePerRequest();
            builder.RegisterType<EntityService>().As<IEntityService>().InstancePerRequest();
            builder.RegisterType<TIMSService>().As<ITIMSService>().InstancePerRequest();
            builder.RegisterType<WorkRequestService>().As<IWorkRequestService>().InstancePerRequest();  
            builder.RegisterType<UserService>().As<IUserService>().InstancePerRequest();
            builder.RegisterType<HomeService>().As<IHomeService>().InstancePerRequest();
            builder.RegisterType<WIPService>().As<IWIPService>().InstancePerRequest();
            builder.RegisterType<WMSService>().As<IWMSService>().InstancePerRequest();
            builder.RegisterType<FGWmsService>().As<IFGWmsService>().InstancePerRequest();
            builder.RegisterType<CreateBuyerQRService>().As<ICreateBuyerQRService>().InstancePerRequest();

            IContainer container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
    public interface IDbFactory : IDisposable
    {
        Entities Init();
    }

    public class DbFactory : Disposable, IDbFactory
    {
        private Entities dbContext;

        public Entities Init()
        {
            return dbContext ?? (dbContext = new Entities());
        }

        protected override void DisposeCore()
        {
            if (dbContext != null)
                dbContext.Dispose();
        }
    }
    public class Disposable : IDisposable
    {
        private bool isDisposed;

        ~Disposable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                DisposeCore();
            }

            isDisposed = true;
        }

        // Ovveride this to dispose custom objects
        protected virtual void DisposeCore()
        {
        }
    }
}

