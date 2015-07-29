﻿namespace SharpArch.Testing.NUnit.NHibernate
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;

    using FluentNHibernate.Automapping;

    using Domain;
    using SharpArch.NHibernate;
    using SharpArch.NHibernate.FluentNHibernate;

    using global::NHibernate;
    using global::NHibernate.Tool.hbm2ddl;

    using Configuration = global::NHibernate.Cfg.Configuration;

    /// <summary>
    ///     Provides helper methods for consolidating duplicated code from test fixture base classes.
    /// </summary>
    public static class RepositoryTestsHelper
    {

        public static void FlushAndEvict(this ISession session, object instance)
        {
            // Commits any changes up to this point to the database
            session.Flush();

            // Evicts the instance from the current session so that it can be loaded during testing;
            // this gives the test a clean slate, if you will, to work with
            session.Evict(instance);
        }

        [CLSCompliant(false)]
        public static AutoPersistenceModel GetAutoPersistenceModel(string[] assemblies)
        {
            return (from asmName in assemblies
                    select Assembly.Load(asmName)
                    into asm 
                    from asmType in asm.GetTypes()
                    where typeof(IAutoPersistenceModelGenerator).IsAssignableFrom(asmType)
                    select Activator.CreateInstance(asmType) as IAutoPersistenceModelGenerator
                    into generator 
                    select generator.Generate()).FirstOrDefault();
        }

        public static string[] GetMappingAssemblies()
        {
            var mappingAssembliesSetting = ConfigurationManager.AppSettings["nhibernate.mapping.assembly"];

            Check.Require(
                !string.IsNullOrEmpty(mappingAssembliesSetting), 
                "Please add an AppSetting to your app.config for 'nhibernate.mapping.assembly.' This setting " +
                "takes a comma delimited list of assemblies containing NHibernate mapping files. Including '.dll' " +
                "at the end of each is optional.");

            return mappingAssembliesSetting.Split(',');
        }

        public static ISession InitializeDatabase()
        {
            var cfg = InitializeNHibernateSession();
            var session = cfg.BuildSessionFactory().OpenSession();
            var connection = session.Connection;
            new SchemaExport(cfg).Execute(false, true, false, connection, null);

            return session;
        }

        public static Configuration InitializeNHibernateSession()
        {
            var mappingAssemblies = GetMappingAssemblies();
            var autoPersistenceModel = GetAutoPersistenceModel(mappingAssemblies);
            
            return new NHibernateSessionFactoryBuilder()
                .AddMappingAssemblies(mappingAssemblies)
                .UseAutoPersitenceModel(autoPersistenceModel)
                .BuildConfiguration();
        }

        public static void Close(ISession session)
        {
            if (session != null)
            {
                session.Dispose();
            }
        }

        public static void Shutdown(ISessionFactory sessionFactory)
        {
            if (sessionFactory != null)
            {
                sessionFactory.Dispose();
            }
        }
    }
}