﻿using CodegenCS.Models.DbSchema;
using NUnit.Framework;
using System;
using System.Linq;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace CodegenCS.Tests.TemplateTests;

/************************************************************************************************************************************************************************/
/// <summary>
/// String template: takes a single model, returns a single interpolated-string which gets written to a single file, and doesn't include any other template.
/// </summary>
class MyPocoTemplate2 : ICodegenStringTemplate<Table>
{
    public FormattableString Render(Table table)
    {
        // This is a "Raw String Literal", available since C# 11 (Requires Visual Studio 2022 17.2+ and requires <LangVersion>preview</LangVersion> in the csproj file)
        // It requires 3 (or more) quotes - so we can use double quotes inside without having to escape
        // Since it started with 2 dollars it means that variables are interpolated using 2 curly braces (instead of the default of 1),
        // following a mustache-like pattern and making it easier to write C#/Java/C code.

        return $$"""
                    /// <summary>
                    /// POCO for Users
                    /// </summary>
                    public class {{table.TableName}}
                    {
                        {{string.Join("\n", table.Columns.Select(c => $$"""public {{c.ClrType}} {{c.ColumnName}} { get; set; }"""))}}
                    }
                    """;

        // Raw String Literals will automatically trim "left padding" (like we've also been doing in CodegenTextWriter for any string)
        // and will automatically remove the last line - which means that if we want to keep a linebreak after the closing braces we should leave an empty line before the ending line
        
        // Raw String Literals are not required (but they help a lot if your template includes quotes and curly braces!):
        // if you're not using C#11/VS2022 you can still use ICodegenStringTemplate returning a regular interpolated string.
    }
}
partial class ICodegenStringTemplateTests : BaseTest
{
    [Test]
    public void Test31()
    {
        var model = base.MyDbSchema;

        var writer = new CodegenTextWriter(); // or you can use:  var ctx = new CodegenContext(); var writer = ctx["YourFile.cs"];
        writer.LoadTemplate<MyPocoTemplate2>().Render(model.Tables[0]);

        Assert_That_Content_IsEqual_To_File(writer, "Users.cs");

        // Compare with the previous template from in 1-ICodegenTemplate\ICodegenTemplateTests
        var writer2 = new CodegenTextWriter();
        writer2.LoadTemplate<MyPocoTemplate>().Render(model.Tables[0]);
        Assert.AreEqual(writer.GetContents(), writer2.GetContents());
    }
}








/************************************************************************************************************************************************************************/
/// <summary>
/// String template: like the previous one but generating all tables and breaking the template into smaller blocks (all getting a submodel and returning a Raw String Literal)
/// </summary>
class MyPocoTemplate3 : ICodegenStringTemplate<DatabaseSchema>
{
    // Raw String Literals again. Isn't it cool?
    public FormattableString Render(DatabaseSchema schema) => $$"""
                    /// Auto-Generated by CodegenCS (https://github.com/CodegenCS/CodegenCS)
                    /// Copyright Rick Drizin (just kidding - this is MIT license - use however you like it!)
                     
                    namespace MyNamespace
                    {
                        {{ schema.Tables.Select(t => RenderTable(t)) }}
                    }
                    """;

    FormattableString RenderTable(Table table) => $$"""
                    /// <summary>
                    /// POCO for Users
                    /// </summary>
                    public class {{ table.TableName }}
                    {
                        {{ table.Columns.Select(c => RenderColumn(table, c)) }}
                    }
                    """;

    FormattableString RenderColumn(Table table, Column column) => $$"""
            /// <summary>
            /// [dbo].[{{ table.TableName }}][{{ column.ColumnName }}] ({{ column.SqlDataType }})
            /// </summary>
            public {{ column.ClrType }} {{ column.ColumnName }} { get; set; }
            """;
}
partial class ICodegenStringTemplateTests : BaseTest
{

    [Test]
    public void Test32()
    {
        var model = base.MyDbSchema;

        var writer = new CodegenTextWriter();
        writer.RemoveWhitespaceFromEmptyLines = false;
        writer.LoadTemplate<MyPocoTemplate3>().Render(model);

        Assert_That_Content_IsEqual_To_File(writer, "MyDatabase.cs");
    }
}
