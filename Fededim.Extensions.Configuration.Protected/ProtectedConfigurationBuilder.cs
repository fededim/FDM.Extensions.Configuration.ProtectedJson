﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace Fededim.Extensions.Configuration.Protected
{
    /// <summary>
    /// IProtectedConfigurationBuilder derives from IConfigurationBuilder and a single method WithProtectedConfigurationOptions used to override the ProtectedConfigurationOptions for a particular provider (e.g. the last one added)
    /// </summary>
    public interface IProtectedConfigurationBuilder : IConfigurationBuilder
    {
        /// <summary>
        /// WithProtectedConfigurationOptions is used to override the ProtectedConfigurationOptions for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        IConfigurationBuilder WithProtectedConfigurationOptions(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber = 1);

        /// <summary>
        /// WithProtectedConfigurationOptions is used to override the ProtectedConfigurationOptions for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="purposeString">a purpose used to create the IDataProtector interface according to Microsoft Data Protection api</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        IConfigurationBuilder WithProtectedConfigurationOptions(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, string purposeString = ProtectedConfigurationBuilder.ProtectedConfigurationBuilderPurpose);
    }


    /// <summary>
    /// ProtectedConfigurationBuilder is an improved ConfigurationBuilder which allows partial or full encryption of configuration values stored inside any possible ConfigurationSource and fully integrated in the ASP.NET Core architecture.
    /// </summary>
    public class ProtectedConfigurationBuilder : IProtectedConfigurationBuilder
    {
        public const String ProtectedConfigurationBuilderPurpose = "ProtectedConfigurationBuilder";
        public static String ProtectedConfigurationBuilderKeyNumberPurpose(int keyNumber) => ProtectedConfigurationBuilderStringPurpose($"Key{keyNumber}");
        public static String ProtectedConfigurationBuilderStringPurpose(string purpose)
        {
            if (String.IsNullOrEmpty(purpose))
                throw new ArgumentNullException(purpose);

            return $"{ProtectedConfigurationBuilderPurpose}.{purpose}";
        }

        public const String DefaultProtectRegexString = "Protect(?<subPurposePattern>(:{(?<subPurpose>.+)})?):{(?<protectData>.+?)}";
        public const String DefaultProtectedRegexString = "Protected(?<subPurposePattern>(:{(?<subPurpose>.+)})?):{(?<protectedData>.+?)}";
        public const String DefaultProtectedReplaceString = "Protected${subPurposePattern}:{${protectedData}}";

        protected ProtectedConfigurationData ProtectedGlobalConfigurationData { get; }

        protected IDictionary<int, ProtectedConfigurationData> ProtectedProviderConfigurationData { get; } = new Dictionary<int, ProtectedConfigurationData>();


        protected readonly List<IConfigurationSource> _sources = new List<IConfigurationSource>();


        // dataProtectionServiceProvider overloads
        public ProtectedConfigurationBuilder(IServiceProvider dataProtectionServiceProvider)
            : this(null, dataProtectionServiceProvider, null, 1)
        {
        }


        public ProtectedConfigurationBuilder(IServiceProvider dataProtectionServiceProvider, int keyNumber = 1, String protectedRegexString = null)
            : this(protectedRegexString, dataProtectionServiceProvider, null, keyNumber)
        {
        }

        public ProtectedConfigurationBuilder(IServiceProvider dataProtectionServiceProvider, string purposeString = ProtectedConfigurationBuilder.ProtectedConfigurationBuilderPurpose, String protectedRegexString = null)
            : this(protectedRegexString, dataProtectionServiceProvider, null, purposeString)
        {
        }



        // dataProtectionConfigureAction overloads
        public ProtectedConfigurationBuilder(Action<IDataProtectionBuilder> dataProtectionConfigureAction)
            : this(null, null, dataProtectionConfigureAction, 1)
        {
        }

        public ProtectedConfigurationBuilder(Action<IDataProtectionBuilder> dataProtectionConfigureAction, int keyNumber = 1, String protectedRegexString = null)
            : this(protectedRegexString, null, dataProtectionConfigureAction, keyNumber)
        {
        }


        public ProtectedConfigurationBuilder(Action<IDataProtectionBuilder> dataProtectionConfigureAction, string purposeString = ProtectedConfigurationBuilder.ProtectedConfigurationBuilderPurpose, String protectedRegexString = null)
            : this(protectedRegexString, null, dataProtectionConfigureAction, purposeString)
        {
        }


        // internal general versions

        protected ProtectedConfigurationBuilder(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber = 1)
        {
            ProtectedGlobalConfigurationData = new ProtectedConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, keyNumber);
        }


        protected ProtectedConfigurationBuilder(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, string purposeString = ProtectedConfigurationBuilder.ProtectedConfigurationBuilderPurpose)
        {
            ProtectedGlobalConfigurationData = new ProtectedConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, purposeString);
        }


        /// <summary>
        /// Returns the sources used to obtain configuration values.
        /// </summary>
        public IList<IConfigurationSource> Sources => _sources;

        /// <summary>
        /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
        /// and the registered <see cref="IConfigurationProvider"/>s.
        /// </summary>
        public IDictionary<String, object> Properties { get; } = new Dictionary<String, object>();




        /// <summary>
        /// Adds a new configuration source.
        /// </summary>
        /// <param name="source">The configuration source to add.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        public virtual IConfigurationBuilder Add(IConfigurationSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _sources.Add(source);
            return this;
        }



        /// <summary>
        /// Builds an <see cref="IConfiguration"/> with keys and values from the set of configuration sources registered in <see cref="Sources"/>.
        /// </summary>
        /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the providers generated by registered configuration sources.</returns>
        public virtual IConfigurationRoot Build()
        {
            var providers = new List<IConfigurationProvider>();
            foreach (IConfigurationSource source in _sources)
            {
                IConfigurationProvider provider = source.Build(this);

                // if we have a custom configuration we move the index from the ConfigurationSource object to the newly created ConfigurationProvider object
                ProtectedProviderConfigurationData.TryGetValue(source.GetHashCode(), out var protectedConfigurationData);
                if (protectedConfigurationData != null)
                {
                    ProtectedProviderConfigurationData[provider.GetHashCode()] = protectedConfigurationData;
                    ProtectedProviderConfigurationData.Remove(source.GetHashCode());
                }

                providers.Add(CreateProtectedConfigurationProvider(provider));
            }
            return new ConfigurationRoot(providers);
        }



        /// <summary>
        /// It's a helper method used to override the ProtectedGlobalConfigurationData for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="keyNumber">a number specifying the index of the key to use</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        public IConfigurationBuilder WithProtectedConfigurationOptions(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, int keyNumber = 1)
        {
            ProtectedProviderConfigurationData[Sources[Sources.Count - 1].GetHashCode()] = new ProtectedConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, keyNumber);

            return this;
        }



        /// <summary>
        /// It's a helper method used to override the ProtectedGlobalConfigurationData for a particular provider (e.g. the last one added)
        /// </summary>
        /// <param name="protectedRegexString">a regular expression which captures the data to be decrypted in a named group called protectedData</param>
        /// <param name="dataProtectionServiceProvider">a service provider configured with Data Protection API, this parameters is mutually exclusive to dataProtectionConfigureAction</param>
        /// <param name="dataProtectionConfigureAction">a configure action to setup the Data Protection API, this parameters is mutually exclusive to dataProtectionServiceProvider</param>
        /// <param name="purposeString">a purpose used to create the IDataProtector interface according to Microsoft Data Protection api</param>
        /// <returns>The <see cref="IConfigurationBuilder"/> interface for method chaining</returns>
        public IConfigurationBuilder WithProtectedConfigurationOptions(String protectedRegexString = null, IServiceProvider dataProtectionServiceProvider = null, Action<IDataProtectionBuilder> dataProtectionConfigureAction = null, string purposeString = ProtectedConfigurationBuilder.ProtectedConfigurationBuilderPurpose)
        {
            ProtectedProviderConfigurationData[Sources[Sources.Count - 1].GetHashCode()] = new ProtectedConfigurationData(protectedRegexString, dataProtectionServiceProvider, dataProtectionConfigureAction, purposeString);

            return this;
        }


        /// <summary>
        /// CreateProtectedConfigurationProvider creates a new ProtectedConfigurationProvider using the composition approach
        /// </summary>
        /// <param name="provider"></param>
        /// <returns>a newer decrypted <see cref="IConfigurationProvider"/> if we have a valid protected configuration, otherwise it returns the existing original undecrypted provider</returns>
        protected IConfigurationProvider CreateProtectedConfigurationProvider(IConfigurationProvider provider)
        {
            // this code is an initial one of when I was thinking of casting IConfigurationProvider to ConfigurationProvider (all MS classes derive from this one)
            // in order to retrieve all configuration keys inside DecryptChildKeys using the Data property without using the recursive "hack" of GetChildKeys 
            // it has been commented because it is not needed anymore, but I keep it as workaround for accessing all configuration keys just in case MS changes the implementation of GetChildKeys "forbidding" the actual way
            //var providerType = provider.GetType();

            //if (!providerType.IsSubclassOf(typeof(ConfigurationProvider)))
            //    return provider;

            // we merge Provider and Global ProtectedDataConfiguration, if it is not valid we return the existing original undecrypted provider
            var actualProtectedConfigurationData = ProtectedProviderConfigurationData.ContainsKey(provider.GetHashCode()) ? ProtectedConfigurationData.Merge(ProtectedGlobalConfigurationData, ProtectedProviderConfigurationData[provider.GetHashCode()]) : ProtectedGlobalConfigurationData;
            if (actualProtectedConfigurationData?.IsValid != true)
                return provider;

            // we use composition to perform decryption of all provider values
            return new ProtectedConfigurationProvider(provider, actualProtectedConfigurationData);
        }
    }
}
