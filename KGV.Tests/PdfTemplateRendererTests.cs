using System.Collections.Generic;
using KGV.Core.Models;
using KGV.Core.Utilities;
using Xunit;

namespace KGV.Tests;

public sealed class PdfTemplateRendererTests
{
    [Fact]
    public void Membership_profile_applies_small_font_and_offset()
    {
        var plan = PdfTemplateRenderer.BuildRenderPlan(new[] { Field("mitglied_email", "text") }, new Dictionary<string, string> { ["mitglied_email"] = new string('a', 31) }, PdfTemplateRenderProfiles.Mitgliedsantrag);
        var operation = Assert.Single(plan); Assert.Equal(6.4, operation.FontSize); Assert.Equal(-1.1, operation.Y, 6);
    }

    [Theory]
    [InlineData("true", "X")]
    [InlineData("TRUE", "X")]
    [InlineData("false", "")]
    [InlineData("", "")]
    public void Checkbox_is_normalized(string value, string glyph) => Assert.Equal(glyph, Assert.Single(PdfTemplateRenderer.BuildRenderPlan(new[] { Field("check", "checkbox") }, new Dictionary<string, string> { ["check"] = value })).Text);

    [Fact]
    public void Multiline_is_clipped_and_signature_is_skipped()
    {
        var fields = new[] { Field("multi", "multiline", height: 8), Field("signature", "signature") };
        var plan = PdfTemplateRenderer.BuildRenderPlan(fields, new Dictionary<string, string> { ["multi"] = "one\ntwo", ["signature"] = "never" });
        Assert.Single(plan); Assert.Equal("one", plan[0].Text);
    }

    private static PdfTemplateFieldSpec Field(string name, string kind, double height = 20) => new(name, kind, 1, 0, 0, 100, height);
}
