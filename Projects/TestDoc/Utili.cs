namespace TestDoc
{
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Serialization;

    using Roslyn.Compilers.CSharp;
    using System.Collections.Generic;
    using System.Linq;

    public class Utili
    {
        public static void TestDocGen(string Dir,string File)
        {
            var tests = new TestDoc();
            tests.TestClass = new List<TestClass>();

            foreach (var sourceFile in Directory.EnumerateFiles(Dir, "*.cs", SearchOption.AllDirectories))
            {

                SyntaxTree syntaxTree = SyntaxTree.ParseFile(sourceFile);
                var testClasses =
                    syntaxTree.GetRoot()
                        .DescendantNodes()
                        .Where(n => n.Kind == SyntaxKind.ClassDeclaration)
                        .Cast<ClassDeclarationSyntax>()
                        .Where(
                            c =>
                            c.AttributeLists.Any(
                                al => al.Attributes.Any(attr => attr.GetText().ToString() == "TestClass")));

                foreach (var testClass in testClasses)
                {
                    var className = testClass.Identifier.ValueText;
                    var classSummary = GetSummary(testClass);
                    var testClassObj = new TestClass
                                           {
                                               Name = className,
                                               Summary = classSummary,
                                               TestMethod = new List<TestMethod>()
                                           };

                    var testMethods =
                        testClass.DescendantNodes()
                            .Where(n => n.Kind == SyntaxKind.MethodDeclaration)
                            .Cast<MethodDeclarationSyntax>()
                            .Where(
                                m =>
                                m.AttributeLists.Any(
                                    al =>
                                    al.Attributes.Any(
                                        att =>
                                        att.Kind == SyntaxKind.Attribute && att.Name.ToFullString() == "TestMethod")));

                    foreach (var testMethod in testMethods)
                    {
                        var name = testMethod.Identifier.ValueText;
                        var summary = GetSummary(testMethod);

                        var testMethodObj = new TestMethod { Name = name, Summary = summary };
                        testClassObj.TestMethod.Add(testMethodObj);
                    }

                    tests.TestClass.Add(testClassObj);
                }
            }
            SerializeToXML(tests, File);
        }

        private static void SerializeToXML(TestDoc tests, string filePath)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(TestDoc));
            using (XmlWriter w = XmlWriter.Create(filePath))
            {
                w.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"TestDoc.xslt\"");
                serializer.Serialize(w, tests);
            }
        }

        private static string GetSummary(SyntaxNode node)
        {
            var comments =
                (node.GetLeadingTrivia().FirstOrDefault(t => t.Kind == SyntaxKind.DocumentationCommentTrivia).GetStructure() as
                 DocumentationCommentTriviaSyntax);

            if (comments == null) return "";

            var summary =
                comments.Nodes.Where(n => n.Kind == SyntaxKind.XmlElement)
                    .Cast<XmlElementSyntax>()
                    .Where(co => co.StartTag.Name.ToFullString() == "summary")
                    .Single()
                    .Content.ToFullString();

            return Regex.Replace(summary, "\r\n[ ]+///", "\r\n").Trim();
        }
    }
}
