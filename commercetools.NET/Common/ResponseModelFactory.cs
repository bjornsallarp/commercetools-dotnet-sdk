using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace commercetools.Common
{
    public class ResponseModelFactory
    {
        #region Properties

        /// <summary>
        /// Creating activators is expensive. By caching them performance for creating 
        /// response model objects increas greatly. Defaults to false.
        /// </summary>
        public bool CacheActivators { get; set; }

        #endregion

        #region Constructors

        public ResponseModelFactory()
        {
            this.CacheActivators = true;
        }

        #endregion

        #region Caching

        private static class DelegateStore<T>
        {
            internal static readonly IDictionary<Type, ActivatorDelegate<T>> Store =
                new ConcurrentDictionary<Type, ActivatorDelegate<T>>();
        }

        private bool TryGetCachedActivator<T>(Type objType, out ActivatorDelegate<T> activator)
        {
            activator = null;
            if (this.CacheActivators)
            {
                DelegateStore<T>.Store.TryGetValue(objType, out activator);
            }
            
            return activator != null;
        }

        private void CacheActivator<T>(Type objType, ActivatorDelegate<T> activator)
        {
            if (this.CacheActivators)
            {
                DelegateStore<T>.Store.Add(objType, activator);
            }
        }

        #endregion

        #region Object creation

        /// <summary>
        /// Object activaator delegate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <returns></returns>
        private delegate T ActivatorDelegate<out T>(params object[] args);

        /// <summary>
        /// Prefix for dynamic method names
        /// </summary>
        private const string MethodNamePrefix = "DM$RESPONSE_MODEL_FACTORY$";

        /// <summary>
        /// Expected name of the constructor argument for response models
        /// </summary>
        private const string ConstructorArgumentName = "data";

        /// <summary>
        /// Create an instance of given type with a constructor argument of type object and name 'data'
        /// </summary>
        /// <typeparam name="T">Type of the response model</typeparam>
        /// <param name="data">Constructor argument 'data'</param>
        /// <returns></returns>
        public T CreateInstance<T>(object data)
        {
            Type objType = typeof(T);
            ActivatorDelegate<T> activator;

            if (TryGetCachedActivator(objType, out activator))
            {
                return activator(data);
            }

            activator = CreateActivator<T>(objType);

            // Cache null values as well. If we cannot create an activator 
            // now we won't be able to next time either
            CacheActivator(objType, activator);

            if (activator == null)
            {
                return default(T);
            }

            return activator(data);
        }

        /// <summary>
        /// This is a high performance il-emitting method for creating instance activators
        /// for classes that take an object constructor argument named 'data'
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objType">Type to </param>
        /// <returns></returns>
        private ActivatorDelegate<T> CreateActivator<T>(Type objType)
        {
            ConstructorInfo constructor;
            if (!TryGetConstructorWithDataParameter(objType, out constructor))
            {
                return null;
            }

            string methodName = string.Concat(MethodNamePrefix, objType.Namespace, "$", objType.Name);
            Type[] parameterTypes = { typeof(object) };
            DynamicMethod dynMethod = new DynamicMethod(methodName, objType, parameterTypes, objType);

            ILGenerator il = dynMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldelem_Ref);
            il.Emit(OpCodes.Castclass, parameterTypes[0]);
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);

            return dynMethod.CreateDelegate(typeof(ActivatorDelegate<T>)) as ActivatorDelegate<T>;
        }

        /// <summary>
        /// Gets the constructor for a type where there is a parameter called 'data' of type 'object'.
        /// </summary>
        /// <param name="type">Type to get constructor for</param>
        /// <param name="constructor">Constructor that match our pattern</param>
        /// <returns>True if a valid constructor was found. Otherwise false.</returns>
        private static bool TryGetConstructorWithDataParameter(Type type, out ConstructorInfo constructor)
        {
            constructor = null;
            ConstructorInfo[] constructors = type.GetConstructors();

            foreach (ConstructorInfo thisConstructor in constructors)
            {
                ParameterInfo[] parameters = thisConstructor.GetParameters();

                if (parameters.Length == 1
                    && parameters[0].ParameterType == typeof(object)
                    && parameters[0].Name.Equals(ConstructorArgumentName))
                {
                    constructor = thisConstructor;
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
