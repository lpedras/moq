﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;
using static Moq.Proxy.Tests.TestHelpers;
using MSBuild = Microsoft.Build.Utilities;

namespace Moq.Proxy.Tests
{
    public class ProxyDiscovererTests
    {
        ITestOutputHelper output;

        public ProxyDiscovererTests(ITestOutputHelper output) => this.output = output;

        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task CanDiscoverProxies(string languageName)
        {
            var (workspace, project) = CreateWorkspaceAndProject(languageName);

            var code = languageName == LanguageNames.CSharp ?
                File.ReadAllText(@"..\..\..\ProxyDiscovererTests.Compile.cs") :
                File.ReadAllText(@"..\..\..\ProxyDiscovererTests.Compile.vb");

            var document = project.AddDocument("test", code);
            project = document.Project;

            var discoverer = new ProxyDiscoverer();

            var proxies = await discoverer.DiscoverProxiesAsync(project);

            Assert.Equal(1, proxies.Count);
        }

        [InlineData(LanguageNames.CSharp)]
        [InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public void CanGenerateProxies(string languageName)
        {
            var (workspace, project) = CreateWorkspaceAndProject(languageName);

            var task = new GenerateProxies
            {
                BuildEngine = new MockBuildEngine(output, true),
                FileExtension = languageName == LanguageNames.CSharp ? ".cs" : ".vb",
                LanguageName = languageName,
                OutputPath = new DirectoryInfo(".").FullName,
                References = ReferencePaths.Paths.Select(x => new MSBuild.TaskItem(x)).ToArray(),
                Sources = new[] {
                    languageName == LanguageNames.CSharp ?
                    new MSBuild.TaskItem(new FileInfo(@"..\..\..\ProxyDiscovererTests.Compile.cs").FullName) :
                    new MSBuild.TaskItem(new FileInfo(@"..\..\..\ProxyDiscovererTests.Compile.vb").FullName)
                }
            };

            Assert.True(task.Execute());
            Assert.Equal(1, task.Proxies.Count());
        }
    }
}
