using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Domain.Tests.Quality;

public sealed class CheckIdTests
{
    [Theory]
    [InlineData("OB-STR-001")]
    [InlineData("OB-CONF-002")]
    [InlineData("OB-REF-001")]
    [InlineData("OB-DOM-001")]
    public void The_planned_check_identifiers_are_valid(string value) =>
        Assert.Equal(value, CheckId.Parse(value).Value);

    [Theory]
    [InlineData("OB-CONF-2")]
    [InlineData("OB-CONF-0002")]
    [InlineData("ob-conf-002")]
    [InlineData("OB-C-002")]
    [InlineData("CONF-002")]
    [InlineData("OB-CONF")]
    [InlineData("")]
    public void A_malformed_identifier_is_rejected(string value) =>
        Assert.Throws<ArgumentException>(() => CheckId.Parse(value));

    [Fact]
    public void TryParse_reports_failure_without_throwing()
    {
        Assert.False(CheckId.TryParse("nonsense", out _));
        Assert.False(CheckId.TryParse(null, out _));
        Assert.True(CheckId.TryParse("OB-CONF-002", out CheckId parsed));
        Assert.Equal("OB-CONF-002", parsed.Value);
    }
}

public sealed class FindingTests
{
    private static readonly CheckId AnyCheck = CheckId.Parse("OB-CONF-002");

    private static FindingTarget AnySourceTarget =>
        FindingTarget.ForSourceResource(SourceResourceId.New());

    [Fact]
    public void A_finding_carries_everything_needed_to_audit_it()
    {
        Finding finding = Finding.Create(
            AnyCheck,
            FindingCategory.Conformance,
            FindingSeverity.Error,
            message: "Staging method is absent.",
            target: AnySourceTarget,
            citation: "mCODE STU4 TNMStageGroup: Observation.method cardinality 1..1",
            expected: "method present",
            actual: "method absent");

        Assert.Equal(AnyCheck, finding.CheckId);
        Assert.Equal(FindingCategory.Conformance, finding.Category);
        Assert.Equal("method absent", finding.Actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_finding_without_a_citation_is_rejected(string citation) =>
        Assert.Throws<ArgumentException>(() => Finding.Create(
            AnyCheck, FindingCategory.Conformance, FindingSeverity.Error,
            message: "Staging method is absent.", target: AnySourceTarget, citation: citation));

    [Fact]
    public void A_finding_without_a_message_is_rejected() =>
        Assert.Throws<ArgumentException>(() => Finding.Create(
            AnyCheck, FindingCategory.Conformance, FindingSeverity.Error,
            message: "  ", target: AnySourceTarget, citation: "somewhere"));

    [Fact]
    public void A_finding_must_name_the_check_that_produced_it() =>
        Assert.Throws<ArgumentException>(() => Finding.Create(
            default, FindingCategory.Conformance, FindingSeverity.Error,
            message: "Staging method is absent.", target: AnySourceTarget, citation: "somewhere"));

    [Fact]
    public void The_same_inputs_produce_the_same_message_so_runs_are_comparable()
    {
        FindingTarget target = AnySourceTarget;

        Finding first = Finding.Create(
            AnyCheck, FindingCategory.Conformance, FindingSeverity.Error,
            "Staging method is absent.", target, "citation");
        Finding second = Finding.Create(
            AnyCheck, FindingCategory.Conformance, FindingSeverity.Error,
            "Staging method is absent.", target, "citation");

        Assert.Equal(first, second);
        Assert.Equal(first.ToString(), second.ToString());
    }
}

public sealed class FindingTargetTests
{
    [Fact]
    public void A_source_resource_target_carries_no_domain_entity_type()
    {
        SourceResourceId id = SourceResourceId.New();

        FindingTarget target = FindingTarget.ForSourceResource(id);

        Assert.Equal(FindingTargetKind.SourceResource, target.Kind);
        Assert.Equal(id.Value, target.Id);
        Assert.Null(target.DomainEntityType);
        Assert.Equal($"SourceResource/{id.Value}", target.ToString());
    }

    [Fact]
    public void A_domain_entity_target_names_its_type()
    {
        Guid id = Guid.NewGuid();

        FindingTarget target = FindingTarget.ForDomainEntity("CancerStaging", id);

        Assert.Equal(FindingTargetKind.DomainEntity, target.Kind);
        Assert.Equal("CancerStaging", target.DomainEntityType);
        Assert.Equal($"CancerStaging/{id}", target.ToString());
    }

    [Fact]
    public void A_domain_entity_target_without_a_type_is_rejected() =>
        Assert.Throws<ArgumentException>(() => FindingTarget.ForDomainEntity("  ", Guid.NewGuid()));
}

public sealed class CoverageNoteTests
{
    [Fact]
    public void A_coverage_note_records_what_was_not_examined_and_why()
    {
        CoverageNote note = CoverageNote.Create(
            subject: "Condition.onset[x] as Age",
            reason: "V1 reads onset stated as a date or a period only.");

        Assert.Equal("Condition.onset[x] as Age", note.Subject);
        Assert.Null(note.Target);
    }

    [Fact]
    public void A_coverage_note_may_name_a_specific_target()
    {
        FindingTarget target = FindingTarget.ForSourceResource(SourceResourceId.New());

        CoverageNote note = CoverageNote.Create("Procedure", "Out of V1 scope.", target);

        Assert.Equal(target, note.Target);
    }

    [Theory]
    [InlineData("", "reason")]
    [InlineData("subject", "")]
    public void A_coverage_note_missing_its_subject_or_reason_is_rejected(string subject, string reason) =>
        Assert.Throws<ArgumentException>(() => CoverageNote.Create(subject, reason));

    [Fact]
    public void A_coverage_note_is_structurally_incapable_of_being_treated_as_a_finding()
    {
        Type note = typeof(CoverageNote);

        Assert.Null(note.GetProperty(nameof(Finding.Severity)));
        Assert.Null(note.GetProperty(nameof(Finding.CheckId)));
        Assert.False(typeof(Finding).IsAssignableFrom(note));
        Assert.Equal(typeof(object), note.BaseType);
    }
}
