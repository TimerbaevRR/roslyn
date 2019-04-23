﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols
{
    public class DocumentSymbolsTests : LanguageServerProtocolTestsBase
    {
        [Fact]
        public async Task TestGetDocumentSymbolsAsync()
        {
            var markup =
@"{|class:class A
{
    {|method:void M()
    {
    }|}
}|}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.DocumentSymbol[]
            {
                CreateDocumentSymbol(LSP.SymbolKind.Class, "A", locations["class"].First())
            };
            CreateDocumentSymbol(LSP.SymbolKind.Method, "M", locations["method"].First(), expected.First());

            var results = await RunGetDocumentSymbolsAsync(solution, true);
            AssertCollectionsEqual(expected, results, AssertDocumentSymbolEquals);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithoutHierarchicalSupport()
        {
            var markup =
@"class {|class:A|}
{
    void {|method:M|}()
    {
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.SymbolInformation[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["class"].First()),
                CreateSymbolInformation(LSP.SymbolKind.Method, "M()", locations["method"].First())
            };

            var results = await RunGetDocumentSymbolsAsync(solution, false);
            AssertCollectionsEqual(expected, results, AssertSymbolInformationsEqual);
        }

        [Fact(Skip = "GetDocumentSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetDocumentSymbolsAsync is updated to support more than 2 levels.
        // https://github.com/dotnet/roslyn/projects/45#card-20033869
        public async Task TestGetDocumentSymbolsAsync__WithLocals()
        {
            var markup =
@"class A
{
    void Method()
    {
        int i = 1;
    }
}";
            var (solution, _) = CreateTestSolution(markup);
            var results = await RunGetDocumentSymbolsAsync(solution, false).ConfigureAwait(false);
            Assert.Equal(results.Length, 3);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NoSymbols()
        {
            var (solution, _) = CreateTestSolution(string.Empty);

            var results = await RunGetDocumentSymbolsAsync(solution, true);
            Assert.Empty(results);
        }

        private static async Task<object[]> RunGetDocumentSymbolsAsync(Solution solution, bool hierarchicalSupport)
        {
            var document = solution.Projects.First().Documents.First();
            var request = new LSP.DocumentSymbolParams
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            var clientCapabilities = new LSP.ClientCapabilities()
            {
                TextDocument = new LSP.TextDocumentClientCapabilities()
                {
                    DocumentSymbol = new LSP.DocumentSymbolSetting()
                    {
                        HierarchicalDocumentSymbolSupport = hierarchicalSupport
                    }
                }
            };

            return await GetLanguageServer(solution).GetDocumentSymbolsAsync(solution, request, clientCapabilities, CancellationToken.None);
        }

        private static void AssertDocumentSymbolEquals(LSP.DocumentSymbol expected, LSP.DocumentSymbol actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Range, actual.Range);
            Assert.Equal(expected.Children.Length, actual.Children.Length);
            for (var i = 0; i < actual.Children.Length; i++)
            {
                AssertDocumentSymbolEquals(expected.Children[i], actual.Children[i]);
            }
        }

        private static LSP.DocumentSymbol CreateDocumentSymbol(LSP.SymbolKind kind, string name, LSP.Location location, LSP.DocumentSymbol parent = null)
        {
            var documentSymbol = new LSP.DocumentSymbol()
            {
                Kind = kind,
                Name = name,
                Range = location.Range,
                Children = new LSP.DocumentSymbol[0]
            };

            if (parent != null)
            {
                var children = parent.Children.ToList();
                children.Add(documentSymbol);
                parent.Children = children.ToArray();
            }

            return documentSymbol;
        }
    }
}
