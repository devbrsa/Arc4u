﻿using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.Dependency.Attribute;
using Arc4u.Dependency.ComponentModel;
using System;
using System.Linq;
using Xunit;

namespace Arc4u.Standard.UnitTest
{
    [Trait("Category", "CI")]
    public class DependencyTests
    {
        #region ComponentModelContainer

        [Fact]
        public void TestCanBeScoped()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\TestParser.json");
            var config = new Config(configuration);


            var container = new ComponentModelContainer();
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.RegisterScoped<IGenerator, IdGenerator>();

            container.CreateContainer();

            var id1 = container.Resolve<IGenerator>().Id;

            using (var c2 = container.CreateScope())
            {
                var id2 = c2.Resolve<IGenerator>().Id;

                Assert.NotEqual(id1, id2);
                Assert.Equal(id2, c2.Resolve<IGenerator>().Id);
                Assert.Equal(id1, container.Resolve<IGenerator>().Id);
                Assert.True(container.CanCreateScope);
            }
        }

        [Fact]
        public void TestCanBeScopedByName()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\TestParser.json");
            var config = new Config(configuration);


            var container = new ComponentModelContainer();
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.RegisterScoped<IGenerator, IdGenerator>("Named");

            container.CreateContainer();

            var id1 = container.Resolve<IGenerator>("Named").Id;

            using (var c2 = container.CreateScope())
            {
                var id2 = c2.Resolve<IGenerator>("Named").Id;

                Assert.NotEqual(id1, id2);
                Assert.Equal(id2, c2.Resolve<IGenerator>("Named").Id);
                Assert.Equal(id1, container.Resolve<IGenerator>("Named").Id);
                Assert.True(container.CanCreateScope);
            }
        }

        [Fact]
        public void TestTryResolve()
        {
            var container = new ComponentModelContainer();
            //container.Register<IGenerator, IdGenerator>();

            container.CreateContainer();

            Assert.False(container.TryResolve<IGenerator>(out var generator));

        }

        [Fact]
        void TestRejectedTypeRegister()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\appsettings.RejectedTypes.json");
            var config = new Config(configuration);
            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.CreateContainer();

            Assert.NotNull(container.Resolve<Config>());
            Assert.Null(container.Resolve<IGenerator>());
        }

        [Fact]
        void TestParser()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\TestParser.json");
            var config = new Config(configuration);
            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.CreateContainer();

            var byInterface = container.Resolve<IGenerator>();
            var byType = container.Resolve<TestParser>();
            Assert.NotNull(byInterface);
            Assert.NotNull(byType);
            Assert.NotEqual(byInterface.Id, byType.Id);
        }

        [Fact]
        void TestScopedParser()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\TestScopedParser.json");
            var config = new Config(configuration);
            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.CreateContainer();

            var byInterface = container.Resolve<IGenerator>();
            var byType = container.Resolve<TestScopedParser>();
            Assert.NotNull(byInterface);
            Assert.NotNull(byType);
            Assert.NotEqual(byInterface.Id, byType.Id);

            using (var scope = container.CreateScope())
            {
                var scopedByInterface = scope.Resolve<IGenerator>();
                var scopedByType = scope.Resolve<TestScopedParser>();
                Assert.NotNull(scopedByInterface);
                Assert.NotNull(scopedByType);
                Assert.NotEqual(scopedByType.Id, scopedByInterface.Id);

                Assert.NotEqual(scopedByInterface.Id, byInterface.Id);
                Assert.NotEqual(scopedByType.Id, byType.Id);
                Assert.Equal(scopedByInterface.Id, scope.Resolve<IGenerator>().Id);
                Assert.Equal(scopedByType.Id, scope.Resolve<TestScopedParser>().Id);
            }
        }



        [Fact]
        void TestCompositionRegister()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
            var config = new Config(configuration);

            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.Register<IGenerator, IdGenerator>();
            container.RegisterSingleton<IGenerator, TestParser>();
            container.RegisterSingleton<IGenerator, SingletonIdGenerator>();
            container.Register<IGenerator, NamedIdGenerator>("Generator1");
            container.RegisterSingleton<IGenerator, NamedSingletonIdGenerator>("Generator2");

            container.CreateContainer();

            Assert.NotNull(container.Resolve<Config>());
            Assert.True(container.ResolveAll<IGenerator>().Count() > 1);
        }

        [Fact]
        void TestNullNamingRegister()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
            var config = new Config(configuration);

            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            Assert.Throws<ArgumentNullException>(() => container.Register<IGenerator, IdGenerator>(null));
        }

        [Fact]
        void TestNullNamingResolver()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
            var config = new Config(configuration);

            var container = new ComponentModelContainer();
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.Register<IGenerator, IdGenerator>();
            container.Register<IGenerator, NamedSingletonIdGenerator>("name");
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IGenerator>());
            Assert.NotNull(container.Resolve<IGenerator>(null));
            Assert.True(container.TryResolve<IGenerator>(out var gen1));
            Assert.True(container.TryResolve<IGenerator>(null, out var gen2));
            Assert.NotNull(container.Resolve(typeof(IGenerator)));
            Assert.NotNull(container.Resolve(typeof(IGenerator), null));
            Assert.True(container.TryResolve(typeof(IGenerator), out var gen3));
            Assert.True(container.TryResolve(typeof(IGenerator), null, out var gen4));
            var t = container.ResolveAll<IGenerator>().ToList();
            Assert.True(container.ResolveAll<IGenerator>().Count() == 1);
            Assert.True(container.ResolveAll<IGenerator>(null).Count() == 1);
            Assert.True(container.ResolveAll(typeof(IGenerator)).Count() == 1);
            Assert.True(container.ResolveAll(typeof(IGenerator), null).Count() == 1);
        }

        [Fact]
        void TestNullNamingMultiResolveException()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
            var config = new Config(configuration);

            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.Register<IGenerator, IdGenerator>();
            container.RegisterSingleton<IGenerator, TestParser>();
            container.RegisterSingleton<IGenerator, SingletonIdGenerator>();
            container.Register<IGenerator, NamedIdGenerator>("Generator1");
            container.RegisterSingleton<IGenerator, NamedSingletonIdGenerator>("Generator2");
            container.CreateContainer();

            Assert.True(container.ResolveAll<IGenerator>().Count() > 1);
            Assert.True(container.ResolveAll<IGenerator>(null).Count() > 1);
            Assert.True(container.ResolveAll(typeof(IGenerator)).Count() > 1);
            Assert.True(container.ResolveAll(typeof(IGenerator), null).Count() > 1);
            //Assert.Throws<MultipleRegistrationException<IGenerator>>(() => container.Resolve<IGenerator>());
            //Assert.Throws<MultipleRegistrationException<IGenerator>>(() => container.Resolve<IGenerator>(null));
            //Assert.False(container.TryResolve<IGenerator>(out var gen1));
            //Assert.False(container.TryResolve<IGenerator>(null, out var gen2));
            //Assert.Throws<MultipleRegistrationException>(() => container.Resolve(typeof(IGenerator)));
            //Assert.Throws<MultipleRegistrationException>(() => container.Resolve(typeof(IGenerator), null));
            //Assert.False(container.TryResolve(typeof(IGenerator), out var gen3));
            //Assert.False(container.TryResolve(typeof(IGenerator), null, out var gen4));
        }

        [Fact]
        void TestComponentModelContainerResolveOnNonRegisteredType()
        {
            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
            var config = new Config(configuration);


            var container = new ComponentModelContainer().InitializeFromConfig(configuration);
            container.RegisterInstance(configuration);
            container.RegisterInstance(config);
            container.CreateContainer();

            Assert.NotNull(container.Resolve<Config>());
            Assert.Null(container.Resolve<IGenerator>());
            Assert.False(container.TryResolve<IGenerator>(out var gen));

        }

        [Fact]
        void TestCreateDelegate()
        {
            ContainerContext.RegisterCreateContainerFunction(() =>
            {
                var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
                var config = new Config(configuration);

                var container = new ComponentModelContainer().InitializeFromConfig(configuration);
                container.RegisterInstance(configuration);
                container.RegisterInstance(config);
                container.CreateContainer();
                return container;
            });

            var configuration = ConfigurationHelper.GetConfigurationFromFile(@"Configs\EmptyAssemblies.json");
            var config = new Config(configuration);

            var container = ContainerContext.CreateContainer();
            Assert.NotNull(container.Resolve<Config>());
            Assert.Null(container.Resolve<IGenerator>());

            Assert.False(container.TryResolve<IGenerator>(out var gen));
        }



        #region Singleton

        [Fact]
        void TestDIRegisterMultipleShareed()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingleton<IGenerator, IdGenerator>();
            container.RegisterSingleton<IGenerator, NamedIdGenerator>();

            container.CreateContainer();
            Assert.Equal(2, container.ResolveAll<IGenerator>().Count());
        }

        [Fact]
        void TestDIRegisterMultipleWithNameShared()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingleton<IGenerator, NamedIdGenerator>("Gen1");
            container.RegisterSingleton<IGenerator, IdGenerator>("Gen1");
            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>("Gen1");
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));
        }

        [Fact]
        void TestDIRegisterMixMultipleShared()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingleton<IGenerator, IdGenerator>();
            container.RegisterSingleton<IGenerator, NamedIdGenerator>();
            container.RegisterSingleton<IGenerator, NamedIdGenerator>("Gen1");
            container.RegisterSingleton<IGenerator, IdGenerator>("Gen1");

            container.CreateContainer();

            Assert.Equal(2, container.ResolveAll<IGenerator>().Count());

            var generators = container.ResolveAll<IGenerator>("Gen1");
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));
        }

        [Fact]
        void TestDIRegisterSingleton()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingleton<IGenerator, IdGenerator>();
            container.RegisterSingleton<IGenerator, IdGenerator>("Gen1");
            container.CreateContainer();

            var idGen1 = container.Resolve<IGenerator>();
            var idGen2 = container.Resolve<IGenerator>("Gen1");
            Assert.NotEqual(idGen1, idGen2);
            Assert.Equal(idGen1, container.Resolve<IGenerator>());
            Assert.Equal(idGen2, container.Resolve<IGenerator>("Gen1"));
        }
        #endregion Singleton

        #region Factory

        [Fact]
        void TestDIRegisterFactoryOfT()
        {
            var container = new ComponentModelContainer();
            container.RegisterFactory<IGenerator>(() => new NamedIdGenerator());
            container.RegisterFactory<IGenerator>(() => new IdGenerator());

            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>().ToList();
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));

            Assert.NotEqual(generators[0].Id, generators[1].Id);
        }

        [Fact]
        void TestDIRegisterFactoryByType()
        {
            var container = new ComponentModelContainer();
            container.RegisterFactory(typeof(IGenerator), () => new NamedIdGenerator());
            container.RegisterFactory(typeof(IGenerator), () => new IdGenerator());

            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>().ToList();
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));

            Assert.NotEqual(generators[0].Id, generators[1].Id);
        }

        [Fact]
        void TestDIRegisterSingletonFactoryOfT()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingletonFactory<IGenerator>(() => new NamedIdGenerator());
            container.RegisterSingletonFactory<IGenerator>(() => new IdGenerator());

            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>().ToList();
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));

            Assert.NotEqual(generators[0].Id, generators[1].Id);
        }

        [Fact]
        void TestDIRegisterSingletonFactoryByType()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingletonFactory(typeof(IGenerator), () => new NamedIdGenerator());
            container.RegisterSingletonFactory(typeof(IGenerator), () => new IdGenerator());

            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>().ToList();
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));

            Assert.NotEqual(generators[0].Id, generators[1].Id);
        }
        [Fact]
        void TestDIRegisterSingletonFactoryOfTIsSingleton()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingletonFactory<IGenerator>(() => new NamedIdGenerator());

            container.CreateContainer();

            Assert.Equal(container.Resolve<IGenerator>().Id, container.Resolve<IGenerator>().Id);
        }

        [Fact]
        void TestDIRegisterSingletonFactoryByTypeIsSingleton()
        {
            var container = new ComponentModelContainer();
            container.RegisterSingletonFactory(typeof(IGenerator), () => new NamedIdGenerator());

            container.CreateContainer();

            Assert.Equal(container.Resolve<IGenerator>().Id, container.Resolve<IGenerator>().Id);
        }
        #endregion


        [Fact]
        void TestDIRegisterMultipleWithName()
        {
            var container = new ComponentModelContainer();
            container.Register<IGenerator, NamedIdGenerator>("Gen1");
            container.Register<IGenerator, IdGenerator>("Gen1");
            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>("Gen1");
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));
        }

        [Fact]
        void TestDIRegisterMultiple()
        {
            var container = new ComponentModelContainer();
            container.Register<IGenerator, IdGenerator>();
            container.Register<IGenerator, NamedIdGenerator>();

            container.CreateContainer();

            Assert.Equal(2, container.ResolveAll<IGenerator>().Count());
        }

        [Fact]
        void TestDIRegisterMixMultiple()
        {
            var container = new ComponentModelContainer();
            container.Register<IGenerator, IdGenerator>();
            container.Register<IGenerator, NamedIdGenerator>();
            container.Register<IGenerator, NamedIdGenerator>("Gen1");
            container.Register<IGenerator, IdGenerator>("Gen1");

            container.CreateContainer();

            Assert.Equal(2, container.ResolveAll<IGenerator>().Count());

            var generators = container.ResolveAll<IGenerator>("Gen1");
            Assert.True(generators.Count() == 2);
            Assert.Collection(generators,
                type1 => Assert.IsType<NamedIdGenerator>(type1),
                type2 => Assert.IsType<IdGenerator>(type2));
        }

        [Fact]
        void TestDIRegisterMefAttribue1()
        {
            var container = new ComponentModelContainer();
            container.Initialize(new[] { typeof(IdGenerator) });
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IGenerator>());
            var id1 = container.Resolve<IGenerator>().Id;
            var id2 = container.Resolve<IGenerator>().Id;
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        void TestDIRegisterMefNamedAttribue1()
        {
            var container = new ComponentModelContainer();
            container.Initialize(new[] { typeof(NamedIdGenerator) });
            container.CreateContainer();

            container.TryResolve<IGenerator>(out var gen);
            Assert.Null(gen);
            Assert.NotNull(container.Resolve<IGenerator>("Generator1"));
            var id1 = container.Resolve<IGenerator>("Generator1").Id;
            var id2 = container.Resolve<IGenerator>("Generator1").Id;
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        void TestDIRegisterMefSingletonAttribue1()
        {
            var container = new ComponentModelContainer();
            container.Initialize(new[] { typeof(SingletonIdGenerator) });
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IGenerator>());
            var id1 = container.Resolve<IGenerator>().Id;
            var id2 = container.Resolve<IGenerator>().Id;
            Assert.Equal(id1, id2);
        }

        [Fact]
        void TestDIRegisterMefNamedSingletonAttribue1()
        {
            var container = new ComponentModelContainer();
            container.Initialize(new[] { typeof(NamedSingletonIdGenerator) });
            container.CreateContainer();

            container.TryResolve<IGenerator>(out var gen);
            Assert.Null(gen);
            Assert.NotNull(container.Resolve<IGenerator>("Generator2"));
            var id1 = container.Resolve<IGenerator>("Generator2").Id;
            var id2 = container.Resolve<IGenerator>("Generator2").Id;
            Assert.Equal(id1, id2);
        }

        [Fact]
        void TestDIRegister()
        {
            var container = new ComponentModelContainer();
            container.Register<IGenerator, IdGenerator>();
            container.Register<IGenerator, IdGenerator>("Gen1");
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IGenerator>());
            Assert.NotNull(container.Resolve<IGenerator>("Gen1"));
        }


        [Fact]
        void TestDIRegisterInstancesOfT()
        {
            var container = new ComponentModelContainer();
            var instance = new IdGenerator();
            container.RegisterInstance<IGenerator>(instance);
            container.RegisterInstance<IGenerator>(new NamedIdGenerator());
            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>().ToList();
            Assert.Equal(2, generators.Count());
            Assert.NotEqual(generators[0].Id, generators[1].Id);
        }

        [Fact]
        void TestDIRegisterInstancesByType()
        {
            var container = new ComponentModelContainer();
            var instance = new IdGenerator();
            container.RegisterInstance(typeof(IGenerator), instance);
            container.RegisterInstance(typeof(IGenerator), new NamedIdGenerator());
            container.CreateContainer();

            var generators = container.ResolveAll<IGenerator>().ToList();
            Assert.Equal(2, generators.Count());
            Assert.NotEqual(generators[0].Id, generators[1].Id);
        }

        [Fact]
        void TestDIRegisterInstances2()
        {
            var container = new ComponentModelContainer();
            var instance = new DualSignletonResolver();
            container.RegisterInstance<IBool1>(instance);
            container.RegisterInstance<IBool2>(instance);
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IBool1>());
            Assert.Equal(container.Resolve(typeof(IBool1)), instance);

            Assert.NotNull(container.Resolve<IBool2>());
            Assert.NotNull(container.Resolve<IBool2>());
            Assert.Equal(container.Resolve(typeof(IBool2)), instance);
        }

        [Fact]
        void TestDIRegisterInstances3()
        {
            var container = new ComponentModelContainer();
            var instance = new SingletonIdGenerator();
            container.RegisterInstance<IGenerator>(instance);
            container.RegisterInstance<IGenerator>(new SingletonIdGenerator(), "Name1");
            container.RegisterInstance<IGenerator>(new SingletonIdGenerator(), "Name2");
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IGenerator>());
            var n1 = container.Resolve(typeof(IGenerator), "Name1");
            var n2 = container.Resolve(typeof(IGenerator), "Name2");
            Assert.NotEqual(n1, instance);
            Assert.NotEqual(n2, instance);
            Assert.NotEqual(n1, n2);
        }

        [Fact]
        void TestDIRegisterInstances4()
        {
            var container = new ComponentModelContainer();
            var instance = new SingletonIdGenerator();
            container.RegisterInstance<IGenerator>(instance);
            container.RegisterInstance<IGenerator>(new SingletonIdGenerator(), "Name");
            container.RegisterInstance<IGenerator>(new SingletonIdGenerator(), "Name");
            container.CreateContainer();

            Assert.NotNull(container.Resolve<IGenerator>());
            Assert.Throws<IndexOutOfRangeException>(() => container.Resolve(typeof(IGenerator), "Name"));
            var instances = container.ResolveAll(typeof(IGenerator), "Name");
            Assert.True(instances.Count() == 2);
            Assert.NotEqual(instances.First(), instances.Last());
        }


        #endregion
    }

    #region Types

    public interface IGenerator
    {
        Guid Id { get; }
    }

    public interface IBool1
    {
        bool IsOk();
    }

    public interface IBool2
    {
        bool IsWrong();
    }

    [Export(typeof(IGenerator))]
    public sealed class IdGenerator : IGenerator, IDisposable
    {
        public IdGenerator()
        {
            _id = Guid.NewGuid();
        }

        private Guid _id;
        public Guid Id => _id;

        public void Dispose()
        {
            _id = Guid.Empty;
        }
    }

    [Export(typeof(IGenerator)), Shared]
    [Export]
    public class TestParser : IGenerator
    {
        public TestParser()
        {
            _id = Guid.NewGuid();
        }

        private Guid _id;
        public Guid Id => _id;
    }

    [Export(typeof(IGenerator)), Dependency.Attribute.Scoped]
    [Export]
    public class TestScopedParser : IGenerator
    {
        public TestScopedParser()
        {
            _id = Guid.NewGuid();
        }

        private Guid _id;
        public Guid Id => _id;
    }


    [Export(typeof(IBool1)), Shared]
    [Export(typeof(IBool2))]
    public class DualSignletonResolver : IBool1, IBool2

    {
        public bool IsOk()
        {
            throw new NotImplementedException();
        }

        public bool IsWrong()
        {
            throw new NotImplementedException();
        }
    }

    [Export(typeof(IGenerator)), Shared]
    public class SingletonIdGenerator : IGenerator
    {
        public SingletonIdGenerator()
        {
            _id = Guid.NewGuid();
        }

        private Guid _id;
        public Guid Id => _id;
    }

    [Export("Generator1", typeof(IGenerator))]
    public class NamedIdGenerator : IGenerator
    {
        public NamedIdGenerator()
        {
            _id = Guid.NewGuid();
        }

        private Guid _id;
        public Guid Id => _id;
    }

    [Export("Generator2", typeof(IGenerator)), Shared]
    public class NamedSingletonIdGenerator : IGenerator
    {
        public NamedSingletonIdGenerator()
        {
            _id = Guid.NewGuid();
        }

        private Guid _id;
        public Guid Id => _id;
    }

    public class Constructor
    {
        public Constructor(IGenerator generator)
        {
            _generator = generator;

        }

        private IGenerator _generator;
        public Second Second { get; set; }
    }

    [Export]
    public class Second
    {

    }

    #endregion
}
