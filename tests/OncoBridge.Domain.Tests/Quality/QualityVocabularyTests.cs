using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Domain.Tests.Quality;

/// <summary>Check identifier format.</summary>
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
    [InlineData("OB-CONF-2")]      // digits not padded
    [InlineData("OB-CONF-0002")]   // too many digits
    [InlineData("ob-conf-002")]    // lowercase
    [InlineData("OB-C-002")]       // area too short
    [InlineData("CONF-002")]       // missing prefix
    [InlineData("OB-CONF")]        // missing number
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

/// <summary>
/// Finding construction, and the separation between findings and coverage notes.
/// </summary>
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

    /// <summary>
    /// A citation is what makes a check auditable and what evidences that it was derived from a
    /// published specification. A finding without one must not be constructible.
    /// </summary>
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

/// <summary>Where findings attach, per ADR-0004.</summary>
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

/// <summary>
/// Coverage notes: recording that something was not examined, which is not a quality problem.
/// </summary>
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

    /// <summary>
    /// The structural guarantee behind the distinction: a coverage note has no severity and shares
    /// no base type with <see cref="Finding"/>, so it cannot be counted among findings even by
    /// accident. Conflating "not examined" with "wrong" is prevented by the type system rather than
    /// by convention.
    /// </summary>
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
