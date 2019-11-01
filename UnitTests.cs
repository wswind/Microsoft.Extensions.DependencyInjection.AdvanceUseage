using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MicrosoftDI.Sample;
using MicrosoftDI.Sample.GenericServices;
using MicrosoftDI.Sample.Services;
using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftDI.Sample
{
    public class UnitTests
    {
        protected readonly ITestOutputHelper Output;
        public UnitTests(ITestOutputHelper  testOutputHelper)
        {
            Output = testOutputHelper;
        }


        [Fact]
        public void Can_Use_Simple_DI()
        {
            diManager diManager = new diManager(sc =>
            {
                sc.AddTransient<ISampleService, SampleService>();
            });
            var serv = diManager.For<ISampleService>();
            int sum = serv.Sum(1, 2);
            Assert.Equal(3, sum);
        }

        [Fact]
        public void Can_Scan_Assembly_Ends_With_Service()
        {
            diManager diManager = new diManager(sc =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                ScanAssemblyEndsService(sc, assembly);
            });
            var serv = diManager.For<ISampleService>();
            Assert.True(serv is SampleService);
            int sum = serv.Sum(1, 2);
            Assert.Equal(3, sum);
        }
        private void ScanAssemblyEndsService(ServiceCollection sc, params Assembly[] assemblies)
        {
            var alltypes = assemblies.SelectMany(x => x.DefinedTypes).Select(x => x.AsType());
            var implTypes = alltypes.Where(x => x.IsClass && !x.IsAbstract && x.Name.EndsWith("Service"));
            foreach (var implType in implTypes)
            {
                var className = implType.Name;
                var servType = implType.GetInterfaces().Where(x => x.Name == $"I{className}").FirstOrDefault();
                if (servType != null)
                {
                    sc.TryAddTransient(servType, implType);
                }
            }
        }
        [Fact]
        public void Can_Register_Generic_Typs()
        {
            var diManager = new diManager(sc =>
            {
                sc.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));
            });
            var serv = diManager.For<IGenericService<int>>();
            Assert.True(serv is GenericService<int>);
            bool equal = serv.Equal(3, 3);
            Assert.True(equal);
        }

        [Fact]
        public void Can_Register_Generic_Interface()
        {
            var diManager = new diManager(sc =>
            {
                sc.AddTransient(typeof(IGenericService<int>), typeof(ExplicitService));
                sc.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));

            });
            var serv = diManager.For<IGenericService<int>>();
            Assert.True(serv is ExplicitService);
            bool equal = serv.Equal(3, 3);
            Assert.True(equal);

            var serv2 = diManager.For<IGenericService<float>>();
            Assert.True(serv2 is GenericService<float>);
            equal = serv2.Equal((float)3.0, (float)3.0);
            Assert.True(equal);
        }
        [Fact]
        public void Can_Resolve_Generic_Caller()
        {
            var diManager = new diManager(sc =>
            {
                sc.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));
                sc.AddTransient(typeof(IGenericService<int>), typeof(ExplicitService));
                sc.AddTransient(typeof(GenericCaller<>));
            });
            var serv = diManager.For<GenericCaller<int>>();
            Assert.True(serv.Serv is ExplicitService);
            Assert.True(serv.Equal(3, 3));

        }

        [Fact]
        public void Can_Not_Resolve_MoreType_Service()
        {
            var diManager = new diManager(sc =>
            {
                sc.AddTransient(typeof(IMoreInTypeService<>), typeof(MoreInTypeService<,>));
            });
            try
            {
                var serv = diManager.For<IMoreInTypeService<int>>();
            }
            catch(Exception ex)
            {
                Assert.True(ex is System.ArgumentException);
                Output.WriteLine(ex.Message);
            }
        }
    }
}
