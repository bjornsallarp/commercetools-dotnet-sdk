using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using commercetools.Common;

namespace commercetools.NET.Perf.Benchmarks
{
    [MemoryDiagnoser]
    public class DynamicModelCreationBenchmark
    {
        private readonly ResponseModelFactory _cachedFactory;
        private readonly ResponseModelFactory _uncachedFactory;
        private readonly Type _modelType;
        private readonly object _constructorArgument = new object();
        
        public DynamicModelCreationBenchmark()
        {
            _cachedFactory = new ResponseModelFactory {CacheActivators = true};
            _uncachedFactory = new ResponseModelFactory();
            _modelType = typeof(TestModelClass);
        }

        [Benchmark(Baseline = true)]
        public TestModelClass CachedResponseModelFactory()
        {
            return _cachedFactory.CreateInstance<TestModelClass>(_constructorArgument);
        }

        [Benchmark]
        public TestModelClass UnCachedResponseModelFactory()
        {
            return _uncachedFactory.CreateInstance<TestModelClass>(_constructorArgument);
        }

        [Benchmark]
        public TestModelClass CompiledLambdaCreation()
        {
            var constructor = Helper.GetConstructorWithDataParameter(_modelType);
            var activator = Helper.GetActivator<TestModelClass>(constructor);
            return activator(_constructorArgument);
        }

        private static class DelegateStore<T>
        {
            internal static readonly IDictionary<Type, Helper.ObjectActivator<T>> Store =
                new ConcurrentDictionary<Type, Helper.ObjectActivator<T>>();
        }

        [Benchmark]
        public TestModelClass CachedCompiledLambdaCreation()
        {
            if (!DelegateStore<TestModelClass>.Store.ContainsKey(_modelType))
            {
                var constructor = Helper.GetConstructorWithDataParameter(_modelType);
                var activator = Helper.GetActivator<TestModelClass>(constructor);
                DelegateStore<TestModelClass>.Store[_modelType] = activator;
            }
            
            return DelegateStore<TestModelClass>.Store[_modelType](_constructorArgument);
        }
    }

    public class TestModelClass
    {
        public TestModelClass(dynamic data)
        {
        }
    }
}