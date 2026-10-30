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

    [Fact]
    public void Lease_profile_keeps_attachment_checkboxes_as_legacy_text_x()
    {
        var plan = PdfTemplateRenderer.BuildRenderPlan(
            new[] { Field("anlage_1", "checkbox", x: 42), Field("regular_check", "checkbox") },
            new Dictionary<string, string> { ["anlage_1"] = "true", ["regular_check"] = "true" },
            PdfTemplateRenderProfiles.Pachtvertrag);

        var legacy = Assert.Single(plan.Where(operation => operation.FieldName == "anlage_1"));
        Assert.Equal("text", legacy.Kind);
        Assert.Equal("X", legacy.Text);
        Assert.Equal(43.8, legacy.X, 6);

        Assert.Equal("checkbox", Assert.Single(plan.Where(operation => operation.FieldName == "regular_check")).Kind);
    }

    [Fact]
    public void Lease_profile_skips_signature_placeholders()
    {
        var plan = PdfTemplateRenderer.BuildRenderPlan(
            new[] { Field("unterschrift_paechter", "signature"), Field("weitere_vereinbarungen", "multiline") },
            new Dictionary<string, string> { ["unterschrift_paechter"] = "never", ["weitere_vereinbarungen"] = "Zeile 1\nZeile 2" },
            PdfTemplateRenderProfiles.Pachtvertrag);

        Assert.DoesNotContain(plan, operation => operation.FieldName == "unterschrift_paechter");
        Assert.Equal(2, plan.Count);
        Assert.All(plan, operation => Assert.Equal("multiline", operation.Kind));
    }

    private static PdfTemplateFieldSpec Field(string name, string kind, double height = 20, double x = 0) => new(name, kind, 1, x, 0, 100, height);
}
