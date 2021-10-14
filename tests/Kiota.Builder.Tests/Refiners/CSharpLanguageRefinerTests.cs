using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class CSharpLanguageRefinerTests {
        private readonly CodeNamespace root = CodeNamespace.InitRootNamespace();
        #region CommonLanguageRefinerTests
        [Fact]
        public void DoesNotEscapesReservedKeywordsForClassOrPropertyKind() {
            // Arrange
            var model = root.AddClass(new CodeClass {
                Name = "break", // this a keyword
                ClassKind = CodeClassKind.Model,
            }).First();
            var property = model.AddProperty(new CodeProperty
            {
                Name = "alias",// this a keyword
                Type = new CodeType
                {
                    Name = "string"
                }
            }).First();
            // Act
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
            // Assert
            Assert.Equal("break", model.Name);
            Assert.DoesNotContain("@", model.Name); // classname will be capitalized
            Assert.Equal("alias", property.Name);
            Assert.DoesNotContain("@", property.Name); // classname will be capitalized
        }
        [Fact]
        public void ConvertsUnionTypesToWrapper() {
            var model = root.AddClass(new CodeClass {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            var union = new CodeUnionType {
                Name = "union",
            };
            union.AddType(new () {
                Name = "type1",
            }, new() {
                Name = "type2"
            });
            var property = model.AddProperty(new CodeProperty {
                Name = "deserialize",
                PropertyKind = CodePropertyKind.Custom,
                Type = union.Clone() as CodeTypeBase,
            }).First();
            var method = model.AddMethod(new CodeMethod {
                Name = "method",
                ReturnType = union.Clone() as CodeTypeBase
            }).First();
            var parameter = new CodeParameter {
                Name = "param1",
                Type = union.Clone() as CodeTypeBase
            };
            var indexer = new CodeIndexer {
                Name = "idx",
                ReturnType = union.Clone() as CodeTypeBase,
            };
            model.SetIndexer(indexer);
            method.AddParameter(parameter);
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root); //using CSharp so the indexer doesn't get removed
            Assert.True(property.Type is CodeType);
            Assert.True(parameter.Type is CodeType);
            Assert.True(method.ReturnType is CodeType);
            Assert.True(indexer.ReturnType is CodeType);
        }
        [Fact]
        public void MovesClassesWithNamespaceNamesUnderNamespace() {
            var graphNS = root.AddNamespace("graph");
            var modelNS = graphNS.AddNamespace("graph.model");
            var model = graphNS.AddClass(new CodeClass {
                Name = "model",
                ClassKind = CodeClassKind.Model
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
            Assert.Single(root.GetChildElements(true));
            Assert.Single(graphNS.GetChildElements(true));
            Assert.Single(modelNS.GetChildElements(true));
            Assert.Equal(modelNS, model.Parent);
        }
        #endregion
        #region CSharp
        [Fact]
        public void DisambiguatePropertiesWithClassNames() {
            var model = root.AddClass(new CodeClass {
                Name = "Model",
                ClassKind = CodeClassKind.Model
            }).First();
            var propToAdd = model.AddProperty(new CodeProperty{
                Name = "model",
                Type = new CodeType {
                    Name = "string"
                }
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
            Assert.Equal("model_prop", propToAdd.Name);
            Assert.Equal("model", propToAdd.SerializationName);
        }
        [Fact]
        public void DisambiguatePropertiesWithClassNames_DoesntReplaceSerializationName() {
            var serializationName = "serializationName";
            var model = root.AddClass(new CodeClass {
                Name = "Model",
                ClassKind = CodeClassKind.Model
            }).First();
            var propToAdd = model.AddProperty(new CodeProperty{
                Name = "model",
                Type = new CodeType {
                    Name = "string"
                },
                SerializationName = serializationName,
            }).First();
            ILanguageRefiner.Refine(new GenerationConfiguration { Language = GenerationLanguage.CSharp }, root);
            Assert.Equal(serializationName, propToAdd.SerializationName);
        }
        #endregion
    }
}
