using commercetools.Common;
using FluentAssertions;
using NUnit.Framework;

namespace commercetools.Tests
{
    /// <summary>
    /// Test dynamic class activation in ResponseModelFactory
    /// </summary>
    [TestFixture]
    public class ResponseModelFactoryTests
    {
        /// <summary>
        /// When the expected constructor signature is missing CreateInstance should return null
        /// </summary>
        /// <param name="withCache">Test with cache on/off</param>
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldReturnNullForClassWithIncorrectConstructorSignature(bool withCache)
        {
            ResponseModelFactory creator = new ResponseModelFactory { CacheActivators = withCache };

            ModelWithIncorrectConstructor instance = creator.CreateInstance<ModelWithIncorrectConstructor>(null);

            instance.Should().BeNull();
        }

        /// <summary>
        /// When the expected constructor signature exists CreateInstance 
        /// should create an instance of that class
        /// </summary>
        /// <param name="withCache">Test with cache on/off</param>
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldReturnInstanceForClassWithConstructorSignature(bool withCache)
        {
            ResponseModelFactory creator = new ResponseModelFactory { CacheActivators = withCache };

            ModelWithCorrectConstructor instance = creator.CreateInstance<ModelWithCorrectConstructor>(null);

            instance.Should().NotBeNull();
        }

        /// <summary>
        /// When the expected constructor signature exists CreateInstance 
        /// should pick that constructor and create an instance of that class
        /// </summary>
        /// <param name="withCache">Test with cache on/off</param>
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldReturnInstanceForClassWithMultipleConstructors(bool withCache)
        {
            ResponseModelFactory creator = new ResponseModelFactory { CacheActivators = withCache };

            ModelWithBothCorrectAndIncorrectConstructor instance =
                creator.CreateInstance<ModelWithBothCorrectAndIncorrectConstructor>(null);

            instance.Should().NotBeNull();
        }

        #region Private classes used for tests

        class ModelWithIncorrectConstructor
        {
            public ModelWithIncorrectConstructor()
            { }
        }

        class ModelWithCorrectConstructor
        {
            public ModelWithCorrectConstructor(dynamic data)
            { }
        }

        class ModelWithBothCorrectAndIncorrectConstructor
        {
            public ModelWithBothCorrectAndIncorrectConstructor()
            { }

            public ModelWithBothCorrectAndIncorrectConstructor(dynamic data)
            { }
        }

        #endregion
    }
}
