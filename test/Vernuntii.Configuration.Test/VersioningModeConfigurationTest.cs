﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vernuntii.Extensions;
using Vernuntii.Extensions.BranchCases;
using Vernuntii.MessageVersioning;
using Vernuntii.VersioningPresets;
using Xunit;

namespace Vernuntii.Configuration
{
    public class VersioningModeConfigurationTest
    {
        private const string VersioningModeStringFileName = "String.yml";
        private const string VersioningModeObjectInvalidFileName = "ObjectInvalid.yml";
        private const string VersioningModeObjectValidFileName = "ObjectValid.yml";

        private static AnyPath Workspace = FilesystemDir / "versioning-mode";
        private static IVersioningPresetManager presetManager = VersioningpresetManager.CreateDefault();

        private static IServiceCollection CreateBranchCasesProviderServices(string fileName)
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddOptions()
                .ConfigureVernuntii(features => features
                    .ConfigureGit(features => features
                        .UseConfigurationDefaults(ConfigurationFixture.Default.FindYamlConfigurationFile(Workspace, fileName))));

            services.AddSingleton(presetManager);
            return services;
        }

        private static IBranchCasesProvider CreateBranchCasesProvider(IServiceCollection services) =>
            services.BuildLifetimeScopedServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IBranchCasesProvider>();

        private static IBranchCasesProvider CreateBranchCasesProvider(string fileName)
        {
            var services = CreateBranchCasesProviderServices(fileName);

            services.ConfigureVernuntii(features => features
                .ConfigureGit(features => features
                    .ConfigureBranchCases(branchCases => branchCases
                        .TryCreateVersioningModeExtension())));

            return CreateBranchCasesProvider(services);
        }

        public static IEnumerable<object[]> VersioningModeStringShouldMatchPresetGenerator()
        {
            foreach (var value in CreateBranchCasesProvider(VersioningModeStringFileName).BranchCases.Values) {
                yield return new object[] {
                     new MessageVersioningModeExtensionOptions(
                         presetManager.GetVersioningPreset(value
                             .GetConfigurationExtension()
                             .GetValue<string>(BranchCaseExtensions.VersioningModeKey))),
                     value.GetVersioningModeExtension()
                 };
            }
        }

        [Theory]
        [MemberData(nameof(VersioningModeStringShouldMatchPresetGenerator))]
        public void VersioningModeStringShouldMatchPreset(
            MessageVersioningModeExtensionOptions expectedExtensionOptions,
            MessageVersioningModeExtensionOptions assumedExtensionOptions) =>
            Assert.Equal(expectedExtensionOptions, assumedExtensionOptions);

        public static IEnumerable<object[]> InvalidVersioningModeObjectShouldThrowGenerator()
        {
            var branchCases = CreateBranchCasesProvider(CreateBranchCasesProviderServices(VersioningModeObjectInvalidFileName)).NestedBranchCases;

            yield return new object[] {
                 nameof(BranchCaseExtensions.VersioningModeObject.IncrementMode),
                 () => CreateVersioningModeExtension(branchCases["OnlyConvention"])
             };

            yield return new object[] {
                 nameof(BranchCaseExtensions.VersioningModeObject.MessageConvention),
                 () => CreateVersioningModeExtension(branchCases["OnlyIncrementMode"])
             };

            static MessageVersioningModeExtensionOptions CreateVersioningModeExtension(IBranchCase branchCase) => branchCase
                .TryCreateVersioningModeExtension(presetManager)
                .GetVersioningModeExtension();
        }

        [Theory]
        [MemberData(nameof(InvalidVersioningModeObjectShouldThrowGenerator))]
        public void InvalidVersioningModeObjectShouldThrow(
            string expectedArgumentExceptionFieldName,
            Func<MessageVersioningModeExtensionOptions> extensionFactory)
        {
            var error = Record.Exception(extensionFactory);
            var argumentException = Assert.IsType<ArgumentException>(error);
            Assert.Contains(expectedArgumentExceptionFieldName, argumentException.Message);
        }

        public static IEnumerable<object[]> ValidVersioningModeObjectShouldMatchGenerator()
        {
            var branchCases = CreateBranchCasesProvider(VersioningModeObjectValidFileName).NestedBranchCases;

            yield return new object[] {
                 new MessageVersioningModeExtensionOptions(VersioningPreset.ConventionalCommitsDelivery),
                 branchCases["OnlyPreset"].GetVersioningModeExtension()
             };

            {
                yield return new object[] {
                     new MessageVersioningModeExtensionOptions(VersioningPreset.Manual with {
                         MessageConvention = VersioningPreset.ConventionalCommitsDelivery.MessageConvention,
                         IncrementMode = VersionIncrementMode.Successive
                     }),
                     branchCases["Mixing"].GetVersioningModeExtension()
                 };
            }
        }

        [Theory]
        [MemberData(nameof(ValidVersioningModeObjectShouldMatchGenerator))]
        public void ValidVersioningModeObjectShouldMatch(
            MessageVersioningModeExtensionOptions expectedExtensionOptions,
            MessageVersioningModeExtensionOptions assumedExtensionOptions) =>
            Assert.Equal(expectedExtensionOptions, assumedExtensionOptions);
    }
}
